using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Valcoin.Models;
using Valcoin.Services;

namespace Valcoin.ViewModels
{
    public partial class SearchResultsViewModel : ObservableObject
    {
        [ObservableProperty]
        private string queryText;

        [ObservableProperty]
        private List<ValcoinBlock> blocks;

        [ObservableProperty]
        private List<Transaction> transactions;

        private IChainService chainService;

        public SearchResultsViewModel()
        {
            chainService ??= GetChainService();
            Populate();
        }

        private async void Populate()
        {
            Blocks = (await chainService.GetAllBlocks()).OrderBy(p => p.BlockNumber).ToList();
        }

        private IChainService GetChainService()
        {
            return App.Current.Services.GetService<IChainService>();
        }
    }
}
