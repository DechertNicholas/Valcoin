using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Valcoin.Models
{
    [PrimaryKey("PreviousTransactionId", "PreviousOutputIndex", "TransactionId")]
    public class TxInput
    {
        /// <summary>
        /// The previous transaction referenced by this Input
        /// </summary>
        public string PreviousTransactionId { get; set; }
        /// <summary>
        /// The selected output of the previous transaction to be used as an input in this transaction.
        /// </summary>
        public int PreviousOutputIndex { get; set; }
        /// <summary>
        /// Public key which, when hashed, matches the <see cref="TxOutput.LockSignature"/> of this selected <see cref="TxOutput"/>.
        /// This key will also be used to verify the <see cref="UnlockSignature"/>.
        /// </summary>
        public byte[] UnlockerPublicKey { get; set; }
        /// <summary>
        /// Signed combination of this transactions BlockNumber and the unlocker's public key.
        /// Proves ownership of the public key which the transaction was sent to (by having
        /// the corresponding private key which signed the data).
        /// </summary>
        public byte[] UnlockSignature { get; set; }

        /// <summary>
        /// Database property to uniquely link this input to a transaction, since most coinbase transactions will be identical.
        /// </summary>
        [JsonIgnore]
        public string TransactionId { get; set; } // this will be null until it is loaded from the database.

        public static implicit operator byte[](TxInput t) => JsonSerializer.SerializeToUtf8Bytes(t);

        public TxInput(string previousTransactionId, int previousOutputIndex, byte[] unlockerPublicKey, byte[] unlockSignature)
        {
            PreviousTransactionId = previousTransactionId;
            PreviousOutputIndex = previousOutputIndex;
            UnlockerPublicKey = unlockerPublicKey;
            UnlockSignature = unlockSignature;
        }
    }
}
