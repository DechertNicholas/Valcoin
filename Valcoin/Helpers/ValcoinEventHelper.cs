using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Valcoin.Helpers
{
    /// <summary>
    /// Custom data class for transferring information in events. 
    /// Used to transfer data to the UI thread and display messages to the user.
    /// </summary>
    public class ValcoinEventHelper : EventArgs
    {
        public string Title { get; set; }
        public string Content { get; set; }
        public string CloseButtonText { get; set; }

        /// <summary>
        /// Params in this constructor will directly tie to a WinUI Dialog box.
        /// </summary>
        /// <param name="title">The title for the dialog.</param>
        /// <param name="content">The content of the dialog message.</param>
        /// <param name="closeButtonText">The text on the (only) close button of the dialog.</param>
        public ValcoinEventHelper(string title, string content, string closeButtonText)
        {
            Title = title;
            Content = content;
            CloseButtonText = closeButtonText;
        }
    }
}
