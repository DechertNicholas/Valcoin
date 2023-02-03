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

        private string hashSpeed = "0";

        public MiningViewModel()
        {
            MinerWorker.WorkerReportsProgress = true;
            MinerWorker.WorkerSupportsCancellation = true;
            MinerWorker.DoWork += BeginMining;
        }

        [RelayCommand]
        public void InvokeMiner()
        {
            if (MinerWorker.IsBusy)
                return;
            MinerWorker.RunWorkerAsync();
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
            Miner.SpeedCalculated += UpdateHashSpeed;
            Miner.Mine();
        }

        private void UpdateHashSpeed(object sender, EventArgs e)
        {
            var temp = Miner.HashSpeed.ToString();
            HashSpeed = temp;
        }
    }
}