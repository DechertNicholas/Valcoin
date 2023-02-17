using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Valcoin.Models
{
    [PrimaryKey("TransactionId", "Index")]
    public class TxOutput
    {
        /// <summary>
        /// The amount of Valcoin to send.
        /// </summary>
        public int Amount { get; set; }
        /// <summary>
        /// The address of the recipient - their hashed public key.
        /// </summary>
        public byte[] LockSignature { get; set; }

        /// <summary>
        /// The transaction this is a part of. Mostly used for DB operations.
        /// </summary>
        [JsonIgnore]
        public string TransactionId { get; set; }
        /// <summary>
        /// The index of this output in the transaction. Ensures the index data is persisted if the DB restores the outputs in reverse order.
        /// </summary>
        [JsonIgnore]
        public string Index { get; set; }

        public static implicit operator byte[](TxOutput t) => JsonSerializer.SerializeToUtf8Bytes(t);

        public TxOutput(string index, int amount, byte[] lockSignature)
        {
            // transactionId is not a part of this, because the resulting id will be dependent on this output's data
            Index = index;
            Amount = amount;
            LockSignature = lockSignature;
        }
    }
}
