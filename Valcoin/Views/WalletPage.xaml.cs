// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Valcoin.Helpers;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Valcoin.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class WalletPage : Page
    { 
        public WalletPage()
        {
            this.InitializeComponent();
            ViewModel.TheDispatcher = this.DispatcherQueue;
            ViewModel.WalletUpdater = Task.Run(() => ViewModel.BalanceScheduler());
            ViewModel.TransactionEvent += DisplayTransactionEventDialog;
        }

        private void TextBox_BeforeTextChanging_NumbersOnly(TextBox sender, TextBoxBeforeTextChangingEventArgs args)
        {
            args.Cancel = args.NewText.Any(c => !char.IsDigit(c));
        }

        private async void DisplayTransactionEventDialog(object sender, TransactionEventHelper e)
        {
            ContentDialog dialog = new()
            {
                Title = e.Title,
                Content = e.Content,
                CloseButtonText = e.CloseButtonText,
                XamlRoot = Content.XamlRoot
            };

            await dialog.ShowAsync();
        }
    }
}
