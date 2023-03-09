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
using System.Transactions;
using Valcoin.Helpers;
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

        public IChainService chainService;
        public event EventHandler<TransactionEventHelper> TransactionEvent;

        /// <summary>
        /// The balance of our wallet.
        /// </summary>
        [ObservableProperty]
        private int balance;
        /// <summary>
        /// The address of the wallet we want to send a transaction to, in string format. Prefix with '0x'.
        /// </summary>
        [ObservableProperty]
        private string recipientAddress = string.Empty;
        /// <summary>
        /// The amount to send to a recipient.
        /// </summary>
        [ObservableProperty]
        private string recipientAmount = string.Empty; // use a string here to avoid defaulting to '0' and displaying it
        

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
            // verify the recipient address
            if (RecipientAddress == string.Empty)
            {
                TransactionEvent.Invoke(null, new(
                    "No recipient",
                    "Please specify a recipient address.",
                    "Ok"));
                return;
            }

            // we require a prefix with 0x to ensure the user has actually copied an address, not some other byte or hash string.
            // this help ensure it will actually go to a person and not be locked away forever on accident.
            if (RecipientAddress.Length != 66 || RecipientAddress[0..2] != "0x")
            {
                TransactionEvent.Invoke(null, new(
                    "Invalid address",
                    "The recipient address is invalid. Ensure the address begins with '0x' and is followed by a 64 character hexadecimal string.",
                    "Ok"));
                return;
            }

            // setup amount
            int amount;
            if (RecipientAmount == string.Empty || !RecipientAmount.Any(c => c != '0'))
            {
                TransactionEvent.Invoke(null, new(
                    "Sending a zero amount",
                    "You cannot send a zero amount.",
                    "Ok"));
                return;
            }
            else
            {
                var parsed = int.TryParse(RecipientAmount, out amount);
                if (!parsed)
                {
                    TransactionEvent.Invoke(null, new(
                    "Invalid amount",
                    "The amount you entered is not a valid amount.",
                    "Ok"));
                    return;
                }
            }

            // check our balance
            if (Balance < amount)
            {
                TransactionEvent.Invoke(null, new(
                    "Insufficient balance",
                    "You cannot send more Valcoin than you own.",
                    "Ok"));
                return;
            }

            chainService ??= SetChainService();

            await chainService.Transact(RecipientAddress, amount);
        }

        public IChainService SetChainService()
        {
            return App.Current.Services.GetService<IChainService>();
        }
    }
}
