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

namespace Valcoin.Services
{
    // https://learn.microsoft.com/en-us/dotnet/framework/network-programming/using-udp-services
    public static class NetworkService
    {
        public static UdpClient Client { get; private set; } = new(listenPort);
        private const int listenPort = 2106;
        private static List<Client> clients = new();
//#if !RELEASE
//        private static string localIP;
//#endif

        public static async void StartListener()
        {
            var service = new StorageService();
            Thread.CurrentThread.Name = "UDP Listener";
            clients = await service.GetClients();
#if !RELEASE
            // 255 is not routable, but should hit all clients on the current subnet (including us, which is what we want)
            // useful for debugging, ingest your own data
            clients.Add(new Client() { Address = IPAddress.Broadcast.ToString(), Port = listenPort });

            // we also need to know our IP, so we don't keep re-ingesting our own data
            //using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            //{
            //    socket.Connect("8.8.8.8", 65530);
            //    IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
            //    localIP = endPoint.Address.ToString();
            //}
#endif
            var remoteEP = new IPEndPoint(IPAddress.Any, 0); // get from any IP sending to any port (to our port listenPort)

            try
            {
                while (true)
                {
                    var result = Client.Receive(ref remoteEP);
                    // utilize a task here so that the listener thread can get back to listening ASAP
                    await Task.Run(() => ParseData(result, remoteEP.Address.ToString(), remoteEP.Port));
                }
            }
            finally
            {
                Client.Close();
            }
        }

        public static void StopListener()
        {
            Client.Close();
        }

        /// <summary>
        /// Relay data to all clients on the network.
        /// </summary>
        /// <param name="data">The data to send (a <see cref="ValcoinBlock"/>, <see cref="Transaction"/>, etc).</param>
        /// <returns></returns>
        public static async Task RelayData(byte[] data)
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
        public static async Task SendData(byte[] data, Client client)
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
        public static async Task ParseData(byte[] result, string clientAddress, int clientPort)
        {
            Thread.CurrentThread.Name = "Network Data Parser";
            var service = new StorageService();
            try
            {
                // try to parse the raw data as json, catching if the data isn't json
                var data = JsonDocument.Parse(result);

                // a block will always contain MerkleRoot and will have transactions, but if MerkleRoot is missing it must just be a transaction
                if (data.RootElement.ToString().Contains("MerkleRoot"))
                {
                    var block = data.Deserialize<ValcoinBlock>();
                    switch (await ValidateBlock(block, new()))
                    {
                        case ValidationCode.Miss_Prev_Block:
                            // TODO: Send message to client asking for block.PreviousBlockHash block
                            // break out of this, as the block will be pending in the validation service and we will handle the requested
                            // block like normal.
                            throw new NotImplementedException();

                        case ValidationCode.Valid:
                            await service.AddBlock(block);
                            await service.AddTxs(block.Transactions);
                            break;
                    }
                }
                else if (data.RootElement.ToString().Contains("TransactionId"))
                {
                    // got a new transaction. Validate it and send it to the miner, if it's active
                    var tx = data.Deserialize<Transaction>();
                    if (await ValidateTx(tx, new StorageService()) == ValidationCode.Valid && Miner.MineBlocks == true)
                        Miner.TransactionPool.Add(tx);
                }

                // regardless of validation outcome, update the client data
                await ProcessClient(clientAddress, clientPort, service);
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

        private static async Task ProcessClient(string clientAddress, int clientPort, StorageService service)
        {
            // if all was successful, add the client to the clients list if not present already
            //var clientEndpoint = new IPEndPoint(clientAddress, clientPort);
            var client = clients.Where(c => c.Address == clientAddress)
                .FirstOrDefault(c => c.Port == clientPort);
            if (client != null)
            {
                // client at this endpoint exists
                client.LastCommunicationUTC = DateTime.UtcNow;
                await service.UpdateClient(client);
            }
            else
            {
                // this is a new connection
                client = new() { Address = clientAddress, Port = clientPort, LastCommunicationUTC = DateTime.UtcNow };
                clients.Add(client);
                await service.AddClient(client);
            }
        }
    }
}
