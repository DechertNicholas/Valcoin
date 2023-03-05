using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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
        public Task WalletUpdater { get; set; }
        public Microsoft.UI.Dispatching.DispatcherQueue TheDispatcher { get; set; }

        /// <summary>
        /// The balance of our wallet.
        /// </summary>
        [ObservableProperty]
        private int balance;
        /// <summary>
        /// The address of the wallet we want to send a transaction to, in string format. Prefix with '0x'.
        /// </summary>
        [ObservableProperty]
        private string recipientAddress;
        /// <summary>
        /// The amount to send to a recipient.
        /// </summary>
        [ObservableProperty]
        private string recipientAmount; // use a string here to avoid defaulting to '0' and displaying it
        

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

        [RelayCommand]
        public async void SendTransaction()
        {
            if (Balance < int.Parse(RecipientAmount))
            {
                await DisplayInsufficientBalanceDialog();
            }
        }

        private async Task DisplayInsufficientBalanceDialog()
        {
            ContentDialog insufficientBalanceDialog = new ContentDialog
            {
                Title = "Insufficient balance",
                Content = "You are trying to send more Valcoin than you own.",
                CloseButtonText = "Ok"
            };

            await insufficientBalanceDialog.ShowAsync();
        }
    }
}
