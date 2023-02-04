using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

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
            Miner.Stop = true;
            MinerWorker.CancelAsync();
        }

        private void BeginMining(object sender, DoWorkEventArgs e)
        {
            Miner.Stop = false;
            Miner.Mine();
        }

        private async Task InvokeUpdateHashSpeedRoutine()
        {
            while (MinerWorker.IsBusy)
            {
                // check the current hash speed every second
                await Task.Delay(1000);
                HashSpeed = Miner.HashSpeed.ToString();
            }
            // cleanup on stop so that we have nice fresh metrics when started again
            HashSpeed = "0";
        }
    }
}