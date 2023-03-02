using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Buffers.Text;
using System.Collections;
using Valcoin.Models;
using System.Threading;
using static Valcoin.Services.ValidationService;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using System.Numerics;
using Microsoft.UI.Xaml.Automation;
using System.Reflection.Metadata.Ecma335;

namespace Valcoin.Services
{
    // https://learn.microsoft.com/en-us/dotnet/framework/network-programming/using-udp-services
    public class NetworkService : INetworkService
    {
        public static UdpClient Client { get; private set; } = new(listenPort);
        /// <summary>
        /// Useful property that shows which network parses are active.
        /// </summary>
        public static ConcurrentBag<Task> ActiveParses { get; private set; } = new();

        // my domain, running a node that will always be online. Used as the first contact on the network
        private static Client rootClientHint = new("nicholasdechert.com", 2106);
        private const int listenPort = 2106;
        private List<Client> clients = new();
        private IChainService chainService;
//#if !RELEASE
//        private static string localIP;
//#endif

        public NetworkService(IChainService chainService)
        {
            this.chainService = chainService;
        }

        public async void StartListener(CancellationToken token)
        {
            Thread.CurrentThread.Name = "UDP Listener";
            clients = await chainService.GetClients();
            if (clients.Count == 0) { clients.Add(rootClientHint); }
#if !RELEASE
            // 255 is not routable, but should hit all clients on the current subnet (including us, which is what we want)
            // useful for debugging, ingest your own data
            //clients.Add(new Client(IPAddress.Broadcast.ToString(), listenPort));

            // we also need to know our IP, so we don't keep re-ingesting our own data
            //using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            //{
            //    socket.Connect("8.8.8.8", 65530);
            //    IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
            //    localIP = endPoint.Address.ToString();
            //}
#endif
            var remoteEP = new IPEndPoint(IPAddress.Any, 0); // get from any IP sending to any port (to our port listenPort)

            await SynchronizeChain();
            await ProliferateClients();

            try
            {
                while (!token.IsCancellationRequested)
                {
                    // remove old parses
                    ActiveParses.Where(t => t.IsCompleted == true).ToList().ForEach(t => ActiveParses.TryTake(out _));
                    var result = await Client.ReceiveAsync(token);
                    // utilize a task here so that the listener thread can get back to listening ASAP
                    var task = Task.Run(async () => await ParseData(result), token);
                    ActiveParses.Add(task);
                }
            }
            catch (Exception ex) when (ex is OperationCanceledException || ex is TaskCanceledException)
            {
                // this is expected
            }
            finally
            {
                Client.Close();
            }
        }

        public void StopListener()
        {
            Client.Close();
        }

        /// <summary>
        /// Relay data to all clients on the network.
        /// </summary>
        /// <param name="data">The data to send (a <see cref="ValcoinBlock"/>, <see cref="Transaction"/>, etc).</param>
        /// <returns></returns>
        public async Task RelayData(byte[] data)
        {
            foreach (var client in clients)
            {
                await SendData(data, client);
            }
        }

        /// <summary>
        /// Send data to an individual client.
        /// </summary>
        /// <param name="data">The data to send (a <see cref="ValcoinBlock"/>, <see cref="Transaction"/>, etc).</param>
        /// <param name="client">The client to send to.</param>
        /// <returns></returns>
        public async Task SendData(byte[] data, Client client)
        {
            // address is test value, will change to have a real param
            await Client.SendAsync(data, client.Address, client.Port);
        }

        /// <summary>
        /// Parses and evaluates the data gotten from the UDP listener. Offloads all work from the listener thread.
        /// </summary>
        /// <param name="result">The bytes returned from the listener.</param>
        /// <param name="clientAddress">The IP address from the listener.</param>
        /// <param name="clientPort">The port from the listener.</param>
        public async Task ParseData(UdpReceiveResult result)
        {
            Thread.CurrentThread.Name = "Network Data Parser";
            try
            {
                // try to parse the raw data as json, catching if the data isn't json
                var clientAddress = result.RemoteEndPoint.Address;
                var clientPort = result.RemoteEndPoint.Port;
                var data = JsonDocument.Parse(result.Buffer);

                // a block will always contain MerkleRoot and will have transactions, but if MerkleRoot is missing it must just be a transaction
                if (data.RootElement.ToString().Contains("MerkleRoot"))
                {
                    var block = data.Deserialize<ValcoinBlock>();
                    switch (ValidateBlock(block))
                    {
                        case ValidationCode.Miss_Prev_Block:
                            // TODO: Send a sync request message to the client
                            var message = new Message(Convert.ToHexString(block.PreviousBlockHash));
                            foreach (var client in clients)
                            {
                                await SendData(message, client);
                            }
                            break;

                        case ValidationCode.Valid:
                            await chainService.AddBlock(block);
                            break;
                    }
                }
                else if (data.RootElement.ToString().Contains("TransactionId"))
                {
                    // got a new transaction. Validate it and send it to the miner, if it's active
                    var tx = data.Deserialize<Transaction>();
                    if (ValidateTx(tx) == ValidationCode.Valid && MiningService.MineBlocks == true)
                        MiningService.TransactionPool.Add(tx);
                }
                else if (data.RootElement.ToString().Contains("MessageType"))
                {
                    var message = data.Deserialize<Message>();
                    var client = new Client(clientAddress.ToString(), clientPort);
                    switch (message.MessageType)
                    {
                        // the client wants to synchronize their chain with ours
                        case MessageType.Sync:
                            ValcoinBlock syncBlock = null;
                            ValcoinBlock ourHighestBlock = await chainService.GetLastMainChainBlock();
                            if (message.HighestBlockNumber == 0 && message.BlockId == "")
                            {
                                // client has no blocks and needs a full sync
                                syncBlock = (await chainService.GetBlocksByNumber(1)).Where(b => b.NextBlockHash != new byte[32]).FirstOrDefault();
                                if (syncBlock == null) break; // we have no blocks either, send nothing
                            }
                            else
                            {
                                syncBlock = await chainService.GetBlock(message.BlockId); // the client's highest block
                                if (syncBlock == null) break; // we don't have this block, send nothing

                                // check if they already are at the last main chain block - same block height as us, and
                                // no next block defined yet
                                if (syncBlock != null &&
                                    syncBlock.NextBlockHash.SequenceEqual(new byte[32]) &&
                                    syncBlock.BlockNumber == ourHighestBlock.BlockNumber)
                                    break; // already sync'd
                            }

                            // get the next block in the chain. We don't really care if we're on the main
                            var nextBlock = await chainService.GetBlock(Convert.ToHexString(syncBlock.NextBlockHash));
                            do
                            {
                                await SendData(nextBlock, client);
                                nextBlock = await chainService.GetBlock(Convert.ToHexString(nextBlock.NextBlockHash));
                            }
                            while (!nextBlock.NextBlockHash.SequenceEqual(new byte[32])); // while not 32 bytes of 0
                            break;

                        // the client is requesting a specific block
                        case MessageType.BlockRequest:
                            var requestBlock = await chainService.GetBlock(message.BlockId);
                            if (requestBlock != null)
                                await SendData(requestBlock, client);
                            break;
                            
                        // the client is requesting we share our list of clients
                        case MessageType.ClientRequest:
                            var clientSend = new Message(await chainService.GetClients());
                            await SendData(clientSend, client);
                            break;

                        case MessageType.ClientShare:
                            var ourClients = await chainService.GetClients();
                            message.Clients.Where(c => ourClients.Contains(c) == false)
                                .ToList()
                                .ForEach(async c => await ProcessClient(c.Address, c.Port));
                            break;
                    }
                }

                // regardless of validation outcome, update the client data
                await ProcessClient(clientAddress.ToString(), clientPort);
            }
            catch (Exception ex)
            {
                if (!ex.Message.Contains("is an invalid start of a value"))
                {
                    // this exception IS NOT a Json formatting exception.
                    // essentially, we don't care if invalid JSON is sent to us as the data it carried is probably invalid anyway
                    // (since this Valcoin client is the only one that exists), or accidentally sent to us by another program.
                    throw;
                }
            }
        }

        public async Task ProcessClient(string clientAddress, int clientPort)
        {
            // if all was successful, add the client to the clients list if not present already
            //var clientEndpoint = new IPEndPoint(clientAddress, clientPort);
            var client = clients.Where(c => c.Address == clientAddress)
                .FirstOrDefault(c => c.Port == clientPort);
            if (client != null)
            {
                // client at this endpoint exists
                client.LastCommunicationUTC = DateTime.UtcNow;
                await chainService.UpdateClient(client);
            }
            else
            {
                // this is a new connection
                client = new(clientAddress, clientPort) { LastCommunicationUTC = DateTime.UtcNow };
                clients.Add(client);
                await chainService.AddClient(client);
            }
        }

        public async Task SynchronizeChain()
        {
            var highestBlock = await chainService.GetLastMainChainBlock();
            // organize the client list by last comm time, then split into chunks of 3, and select the first chunk (top 3 clients)
            var top3 = clients.OrderBy(c => c.LastCommunicationUTC).Chunk(3).ToList()[0].ToList();
            // try to synchronize with the top 3 clients
            for (var i = 0; i < Math.Min(3, top3.Count); i++)
            {
                var client = top3[i];
                Message msg = null;
                if (highestBlock == null)
                {
                    msg = new Message(0, ""); // we have no blocks
                }
                else
                {
                    msg = new Message(highestBlock.BlockNumber, highestBlock.BlockId);
                }
                
                await SendData(msg, client);
            }
        }

        public async Task ProliferateClients()
        {
            // organize the client list by last comm time, then split into chunks of 3, and select the first chunk (top 3 clients)
            var top3 = clients.OrderBy(c => c.LastCommunicationUTC).Chunk(3).ToList()[0].ToList();
            // try to synchronize with the top 3 clients
            for (var i = 0; i < Math.Min(3, top3.Count); i++)
            {
                var client = top3[i];
                var msg = new Message() { MessageType = MessageType.ClientRequest };
                await SendData(msg, client);
            }
        }
    }
}
