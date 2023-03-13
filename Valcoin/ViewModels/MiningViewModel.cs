using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Valcoin.Helpers;
using Valcoin.Models;
using Valcoin.Services;

namespace Valcoin.ViewModels
{
    public partial class MiningViewModel : ObservableObject
    {
        internal BackgroundWorker MinerWorker { get; set; } = new();
        public Microsoft.UI.Dispatching.DispatcherQueue TheDispatcher { get; set; }
        public event EventHandler<ValcoinEventHelper> MiningEvent;

        [ObservableProperty]
        private string hashSpeed = "0";

        public MiningViewModel()
        {
            MinerWorker.WorkerReportsProgress = true;
            MinerWorker.WorkerSupportsCancellation = true;
            MinerWorker.DoWork += BeginMining;
        }

        [RelayCommand]
        public async void InvokeMiner()
        {
            if (MinerWorker.IsBusy)
                return;
            MinerWorker.RunWorkerAsync();
            await InvokeUpdateHashSpeedRoutine();
        }

        [RelayCommand]
        public void InvokeMinerStop()
        {
            MiningService.MineBlocks = false;
            MinerWorker.CancelAsync();
        }

        private async void BeginMining(object sender, DoWorkEventArgs e)
        {
            MiningService.MineBlocks = true;
            var errorPath = await App.Current.Services.GetService<IMiningService>().Mine();
            if (errorPath != string.Empty)
            {
                TheDispatcher.TryEnqueue(() =>
                    MiningEvent.Invoke(null, new(
                        $"Invalid block - {errorPath.Split('\\').Last()}",
                        "The miner has mined an invalid block. The mining process has stopped. Please restart the process if you wish to attempt again.\n\n" +
                        $"The block data has been written out to file: {errorPath}. This can happen if you try to send transactions too fast." +
                        $"Wait for your transactions to be buried under a few blocks before transacting again.",
                        "Ok"))
                );
            }
        }

        private async Task InvokeUpdateHashSpeedRoutine()
        {
            while (MinerWorker.IsBusy)
            {
                // check the current hash speed every second
                await Task.Delay(1000);
                HashSpeed = App.Current.Services.GetService<IMiningService>().HashSpeed.ToString();
            }
            // cleanup on stop so that we have nice fresh metrics when started again
            HashSpeed = "0";
        }
    }
}