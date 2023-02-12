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

namespace Valcoin.Services
{
    // https://learn.microsoft.com/en-us/dotnet/framework/network-programming/using-udp-services
    public static class NetworkService
    {
        public static UdpClient Client { get; private set; } = new(listenPort);
        public static bool ClientIsListening { get; private set; } = false;
        private const int listenPort = 2106;

        public static async void StartListener()
        {
            //var groupEP = new IPEndPoint(IPAddress.Any, listenPort);

            try
            {
                while (true)
                {
                    ClientIsListening = true;
                    var result = await Client.ReceiveAsync();
                    try
                    {
                        var data = JsonDocument.Parse(result.Buffer);

                        if (data.RootElement.ToString().Contains("TxId"))
                        {
                            var tx = data.Deserialize<Transaction>();
                            Miner.TransactionPool.Add(tx);
                        }
                        else if (data.RootElement.ToString().Contains("BlockHash"))
                        {
                            var block = data.Deserialize<ValcoinBlock>();
                        }
                        ClientIsListening = false;
                    }
                    catch (Exception ex)
                    {
                        if (!ex.Message.Contains("is an invalid start of a value"))
                        {
                            // this exception IS NOT a Json formatting exception.
                            throw;
                        }
                    }
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

        public static async Task SendData(byte[] data)
        {
            await Client.SendAsync(data, "10.11.5.255", listenPort);
        }
    }
}
