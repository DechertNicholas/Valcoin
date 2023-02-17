using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Valcoin.Models;
using Valcoin.Services;
using Windows.ApplicationModel.Activation;
using Windows.Devices.Bluetooth.Advertisement;

namespace Valcoin.ViewModels
{
    internal class WalletViewModel
    {
        public Wallet MyWallet { get; set; }

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
        }
    }
}
