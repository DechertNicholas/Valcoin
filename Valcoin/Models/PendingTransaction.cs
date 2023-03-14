using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Valcoin.Models
{
    /// <summary>
    /// A pointer class to a transaction that has not been processed yet. Prevents attempted double spending by the client before the block is processed.
    /// </summary>
    public class PendingTransaction
    {
        [Key]
        public string TransactionId { get; set; }
        public int Amount { get; set; }
        public long CurrentBlockNumber { get; set; }

        /// <summary>
        /// Create a new PendingTransaction.
        /// </summary>
        /// <param name="transactionId">The transaction ID that is pending.</param>
        /// <param name="amount">The amount of output in that transaction.</param>
        /// <param name="currentBlockNumber">The highest committed block number at the time of the transaction request.</param>
        public PendingTransaction(string transactionId, int amount, long currentBlockNumber)
        {
            TransactionId = transactionId;
            Amount = amount;
            CurrentBlockNumber = currentBlockNumber;
        }
    }
}
