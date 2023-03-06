using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Valcoin.Helpers
{
    public class TransactionEventHelper : EventArgs
    {
        public string Title { get; set; }
        public string Content { get; set; }
        public string CloseButtonText { get; set; }


        public TransactionEventHelper(string title, string content, string closeButtonText)
        {
            Title = title;
            Content = content;
            CloseButtonText = closeButtonText;
        }
    }
}
