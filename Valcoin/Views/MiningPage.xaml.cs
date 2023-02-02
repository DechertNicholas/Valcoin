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
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Valcoin.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MiningPage : Page
    {
        internal BackgroundWorker MinerWorker { get; set; } = new();

        public MiningPage()
        {
            this.InitializeComponent();
            MinerWorker.WorkerReportsProgress = false;
            MinerWorker.WorkerSupportsCancellation = true;
            MinerWorker.DoWork += BeginMining;
        }

        private void ButtonStart_Click(object sender, RoutedEventArgs e)
        {
            if (MinerWorker.IsBusy)
                return;
            MinerWorker.RunWorkerAsync();
        }

        private void ButtonStop_Click(object sender, RoutedEventArgs e)
        {
            Miner.Stop = true;
            MinerWorker.CancelAsync();
        }

        private void BeginMining(object sender, DoWorkEventArgs e)
        {
            Miner.Stop = false;
            Miner.Mine();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TestTextBlock.Text = TestTextBox.Text;
        }
    }
}
