using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Valcoin.Models
{
    [PrimaryKey(nameof(TransactionId), nameof(Amount), nameof(LockSignature))] // known issue, if you send the same amount twice to the same address, you won't have a unique key. Not supported.
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

        public static implicit operator byte[](TxOutput t) => JsonSerializer.SerializeToUtf8Bytes(t);

        public TxOutput(int amount, byte[] lockSignature)
        {
            // transactionId is not a part of this, because the resulting id will be dependent on this output's data
            Amount = amount;
            LockSignature = lockSignature;
        }
    }
}
