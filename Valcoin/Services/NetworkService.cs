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
using Windows.Media.Protection.PlayReady;
using System.Buffers;

namespace Valcoin.Services
{
    // https://learn.microsoft.com/en-us/dotnet/framework/network-programming/using-udp-services
    public class NetworkService : INetworkService
    {
        public static TcpListener Listener { get; private set; }
        /// <summary>
        /// Useful property that shows which network parses are active.
        /// </summary>
        public static ConcurrentQueue<Task> ActiveParses { get; private set; } = new();
        public static int ListenPort { get; private set; } = 2106;

        //public const int SIO_UDP_CONNRESET = -1744830452; // used to suppress errors later
        // my domain, running a node that will always be online. Used as the first contact on the network
        private static readonly Client rootClientHint = new("nicholasdechert.com", 2106);
        private static bool initializing = true;
        private static List<Client> clients = new();
        private readonly IChainService chainService;
        private static string localIP;
        //private static int delay = 200;

        public NetworkService(IChainService chainService)
        {
            this.chainService = chainService;
            Listener ??= new(IPAddress.Any, ListenPort);
        }

        /// <summary>
        /// This should only ever be called once across all instances of the class.
        /// </summary>
        /// <param name="token"></param>
        public async void StartListener(CancellationToken token)
        {
            Thread.CurrentThread.Name = "TCP Listener";
            clients = await chainService.GetClients();
            if (clients.Count < 3) { clients.Add(rootClientHint); }
#if !RELEASE
            // for testing, I add two machines on my local network to the client list. This allows me to test connections without
            // needing to use the internet. These do not need to be kept in a database
            clients.Add(new Client("10.11.5.100", ListenPort));
            clients.Add(new Client("10.11.5.101", ListenPort));

            // we also need to know our IP, so we don't keep re-ingesting our own data
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                socket.Connect("8.8.8.8", 65530);
                IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                localIP = endPoint.Address.ToString();
            }
#endif

            try
            {
                if (initializing)
                {
                    // start listening
                    Listener.Start();
                    // we don't care about this task result, just start running it
                    _ = Task.Run(async () => await BootstrapNetwork(), token);
                    initializing = false;
                }

                while (!token.IsCancellationRequested)
                {
                    TcpClient client = await Listener.AcceptTcpClientAsync(token);
                    // utilize a task here so that the listener thread can get back to listening ASAP
                    _ = Task.Run(async () => await ParseData(client), token);
                    //ActiveParses.Enqueue(task);
                }
            }
            catch (Exception ex) when (ex is OperationCanceledException || ex is TaskCanceledException)
            {
                // this is expected
            }
            finally
            {
                Listener.Stop();
            }
        }

        public void StopListener()
        {
            Listener.Stop();
        }

        /// <summary>
        /// Relay data to all clients on the network.
        /// </summary>
        /// <param name="data">The data to send (a <see cref="ValcoinBlock"/>, <see cref="Transaction"/>, etc).</param>
        /// <returns></returns>
        public async Task RelayData(Message msg)
        {
            foreach (var client in clients)
            {
                await SendData(msg, client);
            }
        }

        /// <summary>
        /// Send data to an individual client.
        /// </summary>
        /// <param name="data">The data to send (a <see cref="ValcoinBlock"/>, <see cref="Transaction"/>, etc).</param>
        /// <param name="client">The client to send to.</param>
        /// <returns></returns>
        public async Task SendData(Message msg, Client client)
        {
            // delay execution to ensure the network service is listening
            //Task.Delay(delay).Wait();
            // address is test value, will change to have a real param
            var tcpClient = new TcpClient();
            try
            {
                if (!tcpClient.ConnectAsync(client.Address, client.Port).Wait(2000)) // wait two seconds to try and connect
                {
                    // we didn't connect
                    return;
                }
                if (!tcpClient.Connected) { return; } // not connected, just leave

                var stream = tcpClient.GetStream();
                await stream.WriteAsync((byte[])msg);
            }
            finally
            {
                tcpClient.Close();
            }
            //await Listener.SendAsync(data, client.Address, client.Port);
        }

        /// <summary>
        /// Parses and evaluates the data gotten from the UDP listener. Offloads all work from the listener thread.
        /// </summary>
        /// <param name="result">The bytes returned from the listener.</param>
        /// <param name="clientAddress">The IP address from the listener.</param>
        /// <param name="clientPort">The port from the listener.</param>
        public async Task ParseData(TcpClient tcpClient)
        {
            // remove old parses
            //for (var i = 0; i < ActiveParses.Count; i++)
            //{
                // there isn't a great thread-safe way to do this, so we use a queue and remove each item. If the item is not finished,
                // we re-add it to the queue. If it is finished, we dispose of it. This cleans the queue on each call of ParseData()
                // the variable i is simply an iterator here, and it doesn't matter if the queue shrinks while iterating because we don't
                // access by index. We simply retry until we max out i, which should always be small
                //var taken = ActiveParses.TryDequeue(out Task task);
                //if (!taken) continue; // just skip if we didn't take anything
                //if (task.IsCompleted != true)
                //{
                //    ActiveParses.Enqueue(task);
                //}
                //else
                //{
                //    task.Dispose();
                //}
            //}

            Thread.CurrentThread.Name = "Network Data Parser";
            try
            {
                // try to parse the raw data as json, catching if the data isn't json
                var clientAddress = (tcpClient.Client.RemoteEndPoint as IPEndPoint).Address;
                var clientPort = (tcpClient.Client.RemoteEndPoint as IPEndPoint).Port;
                if (clientAddress.ToString() == localIP) return;

                // create a new version of the IChainService to avoid DBContext issues when working in parallel
                var localService = chainService.GetFreshService();

                // Use a rented buffer to receive data from the client
                int bufferSize = 65535;
                byte[] buffer = ArrayPool<byte>.Shared.Rent(65535);
                int totalBytesRead = 0;

                var stream = tcpClient.GetStream();
                stream.ReadTimeout = 10000; // Set read timeout to 10 seconds

                // get the data
                while (true)
                {
                    int bytesRead = await stream.ReadAsync(buffer);

                    if (bytesRead == 0)
                    {
                        // If no data was received, the client has disconnected
                        break;
                    }

                    // ensure the buffer is large enough to receive all the data
                    totalBytesRead += bytesRead;

                    if (totalBytesRead == bufferSize)
                    {
                        // buffer is full, so resize it to double the size.
                        Array.Resize(ref buffer, bufferSize * 2);
                    }
                }

                // we're done communicating, so close the socket and continue parsing the data
                tcpClient.Close();
                var memory = buffer.AsMemory()[0..totalBytesRead];
                var data = JsonDocument.Parse(memory);

                // all data is transmitted in a message
                var message = data.Deserialize<Message>();
                var client = new Client(clientAddress.ToString(), message.ListenPort);
                switch (message.MessageType)
                {
                    // the client wants to synchronize their chain with ours
                    case MessageType.Sync:
                        ValcoinBlock syncBlock = null;
                        ValcoinBlock ourHighestBlock = await localService.GetLastMainChainBlock();
                        if (message.HighestBlockNumber == 0 && message.BlockId == "")
                        {
                            // client has no blocks and needs a full sync
                            syncBlock = (await localService.GetBlocksByNumber(1)).Where(b => b.NextBlockHash != new byte[32]).FirstOrDefault();
                            if (syncBlock == null) break; // we have no blocks either, send nothing
                            // send the initial block
                            await SendData(new Message(syncBlock, ListenPort), client);
                        }
                        else
                        {
                            syncBlock = await localService.GetBlock(message.BlockId); // the client's highest block
                            if (syncBlock == null) break; // we don't have this block, send nothing

                            // check if they already are at the last main chain block - same block height as us, and
                            // no next block defined yet
                            if (syncBlock != null &&
                                syncBlock.NextBlockHash.SequenceEqual(new byte[32]) &&
                                syncBlock.BlockNumber == ourHighestBlock.BlockNumber)
                                break; // already sync'd
                        }

                        // get the next block in the chain. We don't really care if we're on the main
                        var nextBlock = await localService.GetBlock(Convert.ToHexString(syncBlock.NextBlockHash));
                        do
                        {
                            await SendData(new Message(nextBlock, ListenPort), client);
                            nextBlock = await localService.GetBlock(Convert.ToHexString(nextBlock.NextBlockHash));
                        }
                        while (!nextBlock.NextBlockHash.SequenceEqual(new byte[32])); // while not 32 bytes of 0
                        // the current nextBlock has a NextBlockHash of 0, but we still need to send it
                        await SendData(new Message(nextBlock, ListenPort), client);
                        // now we've sent all blocks
                        break;

                    // the client is requesting a specific block
                    case MessageType.BlockRequest:
                        var requestBlock = await localService.GetBlock(message.BlockId);
                        if (requestBlock != null)
                            await SendData(new Message(requestBlock, ListenPort), client);
                        break;

                    // the client is requesting we share our list of clients
                    case MessageType.ClientRequest:
                        var clientSend = new Message(await localService.GetClients());
                        await SendData(clientSend, client);
                        break;

                    case MessageType.ClientShare:
                        var ourClients = await localService.GetClients();
                        message.Clients.Where(c => ourClients.Contains(c) == false)
                            .ToList()
                            .ForEach(async c => await ProcessClient(c.Address, c.Port));
                        break;

                    case MessageType.BlockShare:
                        {
                            var block = message.Block;
                            switch (ValidateBlock(block))
                            {
                                case ValidationCode.Miss_Prev_Block:
                                    // TODO: Send a sync request message to the client
                                    var returnMessage = new Message(Convert.ToHexString(block.PreviousBlockHash));
                                    // relay to the network
                                    await RelayData(returnMessage);
                                    break;

                                case ValidationCode.Valid:
                                    await localService.AddBlock(block);
                                    break;
                            }
                            break;
                        }

                    case MessageType.TransactionShare:
                        {
                            //got a new transaction.Validate it and send it to the miner, if it's active
                            var tx = message.Transaction;
                            if (ValidateTx(tx) == ValidationCode.Valid && MiningService.MineBlocks == true)
                                MiningService.TransactionPool.Add(tx);
                            break;
                        }
                }

                // regardless of validation outcome, update the client data
                await ProcessClient(clientAddress.ToString(), message.ListenPort);
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
            if (clientAddress == localIP) return;
#if !RELEASE
            if (clientAddress != localIP)
            {
                // when we get the first client on the local network that isn't us, remove the broadcast address.
                // this prevents a lot of duplicate data later when doing debug testing
                clients.Where(c => c.Address == IPAddress.Any.ToString()).ToList().ForEach(c => clients.Remove(c));
            }
#endif
            // create a new version of the IChainService to avoid DBContext issues when working in parallel
            var localService = chainService.GetFreshService();

            var client = clients.Where(c => c.Address == clientAddress)
                .FirstOrDefault(c => c.Port == clientPort);
            if (client != null)
            {
                // client at this endpoint exists
                client.LastCommunicationUTC = DateTime.UtcNow;
                await localService.UpdateClient(client);
            }
            else
            {
                // this is a new connection
                client = new(clientAddress, clientPort) { LastCommunicationUTC = DateTime.UtcNow };
                clients.Add(client);
                await localService.AddClient(client);
            }
        }

        public async Task BootstrapNetwork()
        {
            await ProliferateClients();
            await SynchronizeChain();
        }

        public async Task SynchronizeChain()
        {
            // create a new version of the IChainService to avoid DBContext issues when working in parallel
            var localService = chainService.GetFreshService();
            var highestBlock = await localService.GetLastMainChainBlock();
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
