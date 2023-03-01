using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Valcoin.Models;
using Valcoin.Services;
using Windows.ApplicationModel.Activation;
using Windows.Devices.Bluetooth.Advertisement;

namespace Valcoin.ViewModels
{
    public partial class WalletViewModel : ObservableObject
    {
        public Wallet MyWallet { get; set; }
        [ObservableProperty]
        private int balance;
        //[System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0052:Remove unread private member",
        //    Justification = "This thread needs to keep listening, but never needs to be accessed." +
        //    "There is probably a better way to do this, but this is easy and works for this application.")]
        public Task WalletUpdater { get; set; }
        public Microsoft.UI.Dispatching.DispatcherQueue TheDispatcher { get; set; }

        public WalletViewModel()
        {
            SetWalletAsync();
        }

        public async void SetWalletAsync()
        {
            var service = App.Current.Services.GetService<IChainService>();
            if ((MyWallet = await service.GetMyWallet()) == null)
            {
                MyWallet = Wallet.Create();
                await service.AddWallet(MyWallet);
            }
        }

        public void BalanceScheduler()
        {
            Thread.CurrentThread.Name = "Wallet Balance Updater";
            while (true)
            {
                TheDispatcher.TryEnqueue(async () => Balance = await App.Current.Services.GetService<IChainService>().GetMyBalance());
                Thread.Sleep(1000 * 5); // run every 5 seconds
            }
        }
    }
}
