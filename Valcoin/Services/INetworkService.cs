using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Valcoin.Models;
using Windows.Devices.Bluetooth.Advertisement;

namespace Valcoin.Services
{
    public interface INetworkService
    {
        public static UdpClient Client { get; private set; }
        public static ConcurrentBag<Task> ActiveParses { get; private set; }

        public void StartListener(CancellationToken token);
        public void StopListener();
        public Task RelayData(byte[] data);
        public Task SendData(byte[] data, Client client);
        public Task ParseData(TcpClient client);
        public Task ProcessClient(string clientAddress, int clientPort);

    }
}
