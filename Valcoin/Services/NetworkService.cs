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

namespace Valcoin.Services
{
    // https://learn.microsoft.com/en-us/dotnet/framework/network-programming/using-udp-services
    public static class NetworkService
    {
        public static UdpClient Client { get; private set; } = new(listenPort);
        private const int listenPort = 2106;
        private static List<Client> clients = new();

        public static void StartListener()
        {
            Thread.CurrentThread.Name = "UDP Listener";
            var groupEP = new IPEndPoint(IPAddress.Any, listenPort);

            try
            {
                while (true)
                {
                    var result = Client.Receive(ref groupEP);
                    // utilize a task here so that the listener thread can get back to listening ASAP
                    Task.Run(() => ParseData(result));
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
            Thread.Sleep(1000); //debug
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
            await Client.SendAsync(data, client.Address.ToString(), listenPort);
        }

        public static void ParseData(byte[] result)
        {
            try
            {
                // try to parse the raw data as json, catching if the data isn't json
                var data = JsonDocument.Parse(result);

                // TxId and BlockHash are exclusive to these two classes, so they can be sorted by these terms
                if (data.RootElement.ToString().Contains("TxId"))
                {
                    // got a new transaction. Send it to the miner for validation
                    var tx = data.Deserialize<Transaction>();
                    Miner.TransactionPool.Add(tx);
                }
                else if (data.RootElement.ToString().Contains("BlockHash"))
                {
                    var block = data.Deserialize<ValcoinBlock>();
                    // TODO: validate and save the block
                }
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
    }
}
