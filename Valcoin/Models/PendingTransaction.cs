using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Valcoin.Models
{
    /// <summary>
    /// A pointer class to a transaction that has not been processed yet. Prevents attempted double spending by the client.
    /// </summary>
    public class PendingTransaction
    {
        [Key]
        public string TransactionId { get; set; }
        public int Amount { get; set; }
        public ulong CurrentBlockNumber { get; set; }

        public PendingTransaction(string transactionId, int amount, ulong currentBlockNumber)
        {
            TransactionId = transactionId;
            Amount = amount;
            CurrentBlockNumber = currentBlockNumber;
        }
    }
}
