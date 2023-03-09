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
using Microsoft.UI.Xaml.Documents;

namespace Valcoin.Services
{
    // https://learn.microsoft.com/en-us/dotnet/framework/network-programming/using-udp-services
    public class NetworkService : INetworkService
    {
        public static TcpListener Listener { get; private set; }
        public static int ListenPort { get; private set; } = 2106;

        // my domain, running a node that will always be online. Used as the first contact on the network
        private static readonly Client rootClientHint = new("nicholasdechert.com", 2106);
        private static bool initializing = true;
        private static List<Client> clients = new();
        private readonly IChainService chainService;
        private static string localIP;

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

        public async Task RelayData(Message msg)
        {
            // exclude our local ip by default
            // we'll probably never have this as an option, but since we have to pass something, we pass this
            await RelayData(msg, localIP);
        }

        /// <summary>
        /// Relay data to all clients on the network.
        /// </summary>
        /// <param name="data">The data to send (a <see cref="ValcoinBlock"/>, <see cref="Transaction"/>, etc).</param>
        /// <returns></returns>
        public async Task RelayData(Message msg, string ipToExclude)
        {
            foreach (var client in clients)
            {
                // skip this one
                if (client.Address == ipToExclude)
                    continue;

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
        }

        /// <summary>
        /// Parses and evaluates the data gotten from the UDP listener. Offloads all work from the listener thread.
        /// </summary>
        /// <param name="result">The bytes returned from the listener.</param>
        /// <param name="clientAddress">The IP address from the listener.</param>
        /// <param name="clientPort">The port from the listener.</param>
        public async Task ParseData(TcpClient tcpClient)
        {
            Thread.CurrentThread.Name = "Network Data Parser";
            try
            {
                // try to parse the raw data as json, catching if the data isn't json
                var clientAddress = (tcpClient.Client.RemoteEndPoint as IPEndPoint).Address;
                if (clientAddress.ToString() == localIP) return;

                // create a new version of the IChainService to avoid DBContext issues when working in parallel
                var localService = chainService.GetFreshService();

                var memory = await GetDataFromClient(tcpClient);

                var data = JsonDocument.Parse(memory);

                // all data is transmitted in a message
                var message = data.Deserialize<Message>();
                var client = new Client(clientAddress.ToString(), message.ListenPort);
                switch (message.MessageType)
                {
                    // the client wants to synchronize their chain with ours
                    case MessageType.Sync:
                        await SynchronizeChainWithClient(client, message);
                        break;

                    case MessageType.SyncResponse:
                        await SynchronizeChainWithClient(client, message, tcpClient); // pass the established client this time
                        break;

                    // the client is requesting a specific block
                    case MessageType.BlockRequest:
                        var requestBlock = await localService.GetBlock(message.BlockId);
                        if (requestBlock != null)
                            await SendData(new Message(requestBlock), client);
                        break;

                    // the client is requesting we share our list of clients
                    case MessageType.ClientRequest:
                        var clientSend = new Message(await localService.GetClients(), ListenPort);
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
                            if (ValidateTx(tx) != ValidationCode.Valid)
                                break; // invalid, just quit

                            MiningService.TransactionPool.TryAdd(tx.TransactionId, tx);
                            await chainService.AddPendingTransaction(tx);

                            // send to the rest of the network
                            await RelayData(message, clientAddress.ToString());

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
            finally
            {
                // we're done communicating, so close the socket and continue parsing the data
                if (tcpClient.Connected)
                    tcpClient.Close();
            }
        }

        public async Task<Memory<byte>> GetDataFromClient(TcpClient tcpClient)
        {
            // Use a rented buffer to receive data from the client
            int bufferSize = 65535;
            byte[] buffer = ArrayPool<byte>.Shared.Rent(65535);
            int totalBytesRead = 0;

            var stream = tcpClient.GetStream();
            stream.ReadTimeout = 30000; // Set read timeout to 30 seconds

            // get the data
            do
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
            while (stream.DataAvailable);

            return buffer.AsMemory()[0..totalBytesRead];
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
            await SendSyncRequests();
        }

        public async Task SynchronizeChainWithClient(Client client, Message syncMessage, TcpClient tcpClient = null)
        {
            if (syncMessage.MessageType == MessageType.Sync)
            {
                // the client sent out sync requests to multiple clients, then closed the connection.
                // we need to establish a new connection that does not stop
                tcpClient = new();
                if (!tcpClient.ConnectAsync(client.Address, client.Port).Wait(2000)) // wait two seconds to try and connect
                {
                    // we didn't connect
                    return;
                }
                if (!tcpClient.Connected) { tcpClient.Close(); } // not connected, just leave

                var stream = tcpClient.GetStream();
                // inform the client we're willing to sync
                await stream.WriteAsync((byte[])new Message(MessageType.SyncResponse));
                // wait for the clien to confirm they're connected and ready
                var response = await GetDataFromClient(tcpClient);
                if (response.Length != 1 || response.Span[0] != 1)
                {
                    // something is malformed, exit
                    tcpClient.Close();
                }

                var localService = chainService.GetFreshService();
                ValcoinBlock syncBlock = null;
                ValcoinBlock ourHighestBlock = await localService.GetLastMainChainBlock();

                // we send our highest block so the client knows when to no longer expect data. We will send this again later when the
                // client is actually ready to validate it
                await stream.WriteAsync((byte[])new Message(ourHighestBlock));
                // wait for the clien to confirm they're connected and ready
                response = await GetDataFromClient(tcpClient);
                if (response.Length != 1 || response.Span[0] != 1)
                {
                    // something is malformed, exit
                    tcpClient.Close();
                }

                if (syncMessage.HighestBlockNumber == 0 && syncMessage.BlockId == "")
                {
                    // client has no blocks and needs a full sync.
                    // get the first block in the chain
                    syncBlock = (await localService.GetBlocksByNumber(1)).Where(b => b.NextBlockHash != new byte[32]).FirstOrDefault();
                    if (syncBlock == null) tcpClient.Close(); // we have no blocks either, send nothing
                    // send the initial block
                    await stream.WriteAsync((byte[])new Message(syncBlock));
                }
                else
                {
                    syncBlock = await localService.GetBlock(syncMessage.BlockId); // the client's highest block
                    if (syncBlock == null) tcpClient.Close(); // we don't have this block, send nothing

                    // check if they already are at the last main chain block - same block height as us, and
                    // no next block defined yet
                    if (syncBlock != null &&
                        syncBlock.NextBlockHash.SequenceEqual(new byte[32]) &&
                        syncBlock.BlockNumber == ourHighestBlock.BlockNumber)
                        tcpClient.Close(); // already sync'd
                }

                

                // get the next block in the chain. We don't really care if we're on the main
                var nextBlock = await localService.GetBlock(Convert.ToHexString(syncBlock.NextBlockHash));
                do
                {
                    // wait for the client to be ready each time
                    response = await GetDataFromClient(tcpClient);
                    if (response.Length != 1 || response.Span[0] != 1)
                    {
                        // something is malformed, exit
                        tcpClient.Close();
                    }
                    await stream.WriteAsync((byte[])new Message(nextBlock));
                    nextBlock = await localService.GetBlock(Convert.ToHexString(nextBlock.NextBlockHash));
                }
                while (!nextBlock.NextBlockHash.SequenceEqual(new byte[32])); // while not 32 bytes of 0

                // wait once more
                response = await GetDataFromClient(tcpClient);
                if (response.Length != 1 || response.Span[0] != 1)
                {
                    // something is malformed, exit
                    tcpClient.Close();
                }
                // the current nextBlock has a NextBlockHash of 0, but we still need to send it
                await stream.WriteAsync((byte[])new Message(nextBlock));
                // now we've sent all blocks
                tcpClient.Close();
            }
            else if (syncMessage.MessageType == MessageType.SyncResponse)
            {
                if (syncMessage.Block == null)
                {
                    tcpClient.Close();
                    return;
                }

                var localService = chainService.GetFreshService();
                var stream = tcpClient.GetStream();
                // say we're ready to start sync
                await stream.WriteAsync(new byte[] {1}); // just a general value that we wouldn't normally get

                // get the highest block to expect
                var memoryHighest = await GetDataFromClient(tcpClient);
                var dataHighest = JsonDocument.Parse(memoryHighest);
                var highestBlockNumber = dataHighest.Deserialize<Message>().Block.BlockNumber;
                var finished = false;

                // begin the sync
                await stream.WriteAsync(new byte[] { 1 });

                do
                {
                    var memory = await GetDataFromClient(tcpClient);
                    var data = JsonDocument.Parse(memory);

                    // all data is transmitted in a message
                    var message = data.Deserialize<Message>();
                    if (ValidateBlock(message.Block) == ValidationCode.Valid)
                    {
                        await localService.AddBlock(message.Block);
                    }
                    if (message.Block.BlockNumber == highestBlockNumber)
                    {
                        finished = true;
                        break;
                    }
                    await stream.WriteAsync(new byte[] { 1 });
                }
                while (!finished);
                tcpClient.Close();
            }
        }

        public async Task SendSyncRequests()
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
                var msg = new Message(MessageType.ClientRequest);
                await SendData(msg, client);
            }
        }
    }
}
