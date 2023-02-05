using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Valcoin.Models
{
    internal class Transaction
    {
        public string TxId { get; set; }

        public string PreviousTxId { get; set; }

        public string SenderAddress { get; set; }

        public string ReceiverPublicKey { get; set; }

        public int Amount { get; set; }

        public byte[] Signature { get; set; }

        // use inputs and outputs as bitcoin does, create hash models for this data.
    }
}
