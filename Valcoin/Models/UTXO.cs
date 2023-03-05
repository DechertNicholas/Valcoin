using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Valcoin.Models
{
    /// <summary>
    /// An Unspent Transaction Output. This is a sort of "shortcut pointer" to a transaction that pays out to the user. Instead of querying the
    /// entire database of transactions and looking for the ones that pay out to us, we can look only in the UTXO table as they will ONLY be assigned to us.
    /// This ensures transactions remain easy to send even when the chain gets very long.
    /// </summary>
    [PrimaryKey("TransactionId", "OutputIndex")]
    public class UTXO
    {
        public string TransactionId { get; set; }
        public int OutputIndex { get; set; }
        public int Amount { get; set; }
        
        public UTXO(string transactionId, int outputIndex, int amount)
        {
            TransactionId = transactionId;
            OutputIndex = outputIndex;
            Amount = amount;
        }
    }
}
