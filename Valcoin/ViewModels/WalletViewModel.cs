using CommunityToolkit.Mvvm.ComponentModel;
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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0052:Remove unread private member",
            Justification = "This thread needs to keep listening, but never needs to be accessed." +
            "There is probably a better way to do this, but this is easy and works for this application.")]
        private Task walletUpdater;

        public WalletViewModel()
        {
            SetWalletAsync();
        }

        public async void SetWalletAsync()
        {
            var service = new StorageService();
            if ((MyWallet = await service.GetMyWallet()) == null)
            {
                MyWallet = Wallet.Create();
                await service.AddWallet(MyWallet);
            }
            walletUpdater = Task.Run(() => UpdateBalance());
        }

        public void UpdateBalance()
        {
            Thread.CurrentThread.Name = "Wallet Balance Updater";
            while (true)
            {
                Thread.Sleep(1000 * 60); // run each minute
                Balance = new StorageService().GetMyWallet().Result.Balance;
            }
        }
    }
}
