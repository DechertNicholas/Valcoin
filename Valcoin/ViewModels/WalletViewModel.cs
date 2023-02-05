using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Valcoin.Models;
using Valcoin.Services;
using Windows.Devices.Bluetooth.Advertisement;

namespace Valcoin.ViewModels
{
    internal class WalletViewModel
    {
        public Wallet MyWallet { get; set; }

        public WalletViewModel()
        {
            if ((MyWallet = StorageService.GetMyWallet()) == null)
            {
                MyWallet = new();
                MyWallet.Initialize();
                StorageService.AddWallet(MyWallet);
            }
            MyWallet.Initialize();
        }
    }
}
