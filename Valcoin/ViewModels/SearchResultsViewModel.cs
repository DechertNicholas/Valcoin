using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Valcoin.ViewModels
{
    public partial class SearchResultsViewModel : ObservableObject
    {
        [ObservableProperty]
        private string queryText;
    }
}
