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
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Valcoin.Helpers;
using Valcoin.Models;
using Valcoin.Services;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Valcoin.Views
{
    /// <summary>
    /// The page dedicated to controlling the <see cref="MiningService"/> and viewing related mining status.
    /// </summary>
    public sealed partial class MiningPage : Page
    {
        public MiningPage()
        {
            this.InitializeComponent();
            MiningService.MiningEvent += DisplayTransactionEventDialog;
        }

        private async void DisplayTransactionEventDialog(object sender, ValcoinEventHelper e)
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
