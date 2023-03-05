using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Valcoin.Services;

namespace Valcoin.ViewModels
{
    public partial class MiningViewModel : ObservableObject
    {
        internal BackgroundWorker MinerWorker { get; set; } = new();

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

        private void BeginMining(object sender, DoWorkEventArgs e)
        {
            MiningService.MineBlocks = true;
            App.Current.Services.GetService<IMiningService>().Mine();
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