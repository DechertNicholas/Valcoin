using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Valcoin.Models;
using Valcoin.Services;
using Windows.Globalization;

namespace Valcoin.ViewModels
{
    public partial class ReportsViewModel : ObservableObject
    {
        [ObservableProperty]
        private List<WealthModel> reportWealthResult = new();

        [ObservableProperty]
        private List<LargeTxModel> reportLargestTransactions = new();

        [ObservableProperty]
        private DateTime lastReportTime = DateTime.Now;

        private IChainService chainService;

        public ReportsViewModel()
        {
            chainService ??= App.Current.Services.GetService<IChainService>();

            var result = chainService.GetAllAddressWealth().Result
                .OrderByDescending(w => w.Value)
                .ThenBy(w => w.Key)
                .ToDictionary(w => w.Key, w => w.Value);
            foreach (var key in result.Keys)
            {
                ReportWealthResult.Add(new() { Address = key, Wealth = result[key] });
            }


            chainService.GetAllMainChainTransactions().Result
                .OrderByDescending(t => t.Outputs.Sum(o => o.Amount))
                .ThenBy(t => t.TransactionId)
                .ToList()
                .ForEach(t => ReportLargestTransactions.Add(new() { TransactionId = t.TransactionId, Amount = t.Outputs.Sum(o => o.Amount) }));
        }
    }

    public class WealthModel
    {
        [Required]
        public string Address { get; set; }

        [Required]
        public int Wealth { get; set; }
    }

    public class LargeTxModel
    {
        [Required]
        public string TransactionId { get; set; }

        [Required]
        public int Amount { get; set; }
    }
}
