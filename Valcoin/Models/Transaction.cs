using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Valcoin.Helpers;

namespace Valcoin.Models
{
    public class Transaction
    {
        /// <summary>
        /// The hash of the transaction in hex string format.
        /// </summary>
        [Key]
        [JsonIgnore]
        public string TransactionId { get; private set; }
        /// <summary>
        /// The version of transaction data formatting being used.
        /// </summary>
        public int Version { get; set; } = 1;
        /// <summary>
        /// <see cref="TxInput"/>s used for this transaction. Can include any number of <see cref="TxInput"/>s so long as the resulting value of all 
        /// <see cref="TxInput"/>s is greater than or equal to the <see cref="TxOutput"/>s for this transaction.
        /// </summary>
        public List<TxInput> Inputs
        {
            get => _inputs;
            set
            {
                _inputs = value.OrderBy(i => i.PreviousTransactionId).ThenBy(i => i.PreviousOutputIndex).ToList();

            }
        }
        /// <summary>
        /// The outputs of this transaction. Max 2 - the output sent to the recipient, and any change that goes back to the sender.
        /// </summary>
        public List<TxOutput> Outputs
        {
            get => _outputs;
            set
            {
                _outputs = value.OrderBy(o => Convert.ToHexString(o.Address)).ThenBy(o => o.Amount).ToList();

            }
        }
        /// <summary>
        /// The block in which this transaction was in. Part of the unlock signature.
        /// </summary>
        public ulong BlockNumber { get; set; }

        private List<TxInput> _inputs = new();
        private List<TxOutput> _outputs = new();

        /// <summary>
        /// Byte[] serializer used for transferring this transaction over the network.
        /// </summary>
        /// <param name="t"></param>
        public static implicit operator byte[](Transaction t) => JsonSerializer.SerializeToUtf8Bytes(t);

        /// <summary>
        /// Constructor used by Entity Framework Core. Don't delete this.
        /// </summary>
        /// <param name="transactionId"></param>
        /// <param name="blockNumber"></param>
        private Transaction(string transactionId, ulong blockNumber)
        {
            TransactionId = transactionId;
            BlockNumber = blockNumber;
        }

        public Transaction(List<TxInput> inputs, List<TxOutput> outputs)
        {
            Inputs = inputs.OrderBy(i => i.PreviousTransactionId).ThenBy(i => i.PreviousOutputIndex).ToList();
            Outputs = outputs.OrderBy(o => Convert.ToHexString(o.Address)).ThenBy(o => o.Amount).ToList();

            TransactionId = GetTxIdAsString();
        }

        /// <summary>
        /// The constructor used by other classes to build a new transaction.
        /// </summary>
        /// <param name="blockNumber">The BlockNumber this transaction is in. Used for DB relations.</param>
        /// <param name="inputs">The group of inputs for this transaction.</param>
        /// <param name="outputs">The group of outputs for this transaction.</param>
        [JsonConstructor] // for serialization over the network
        public Transaction(ulong blockNumber, List<TxInput> inputs, List<TxOutput> outputs)
        {
            if (outputs.Distinct(new TxOutputComparer()).Count() != outputs.Count)
                throw new InvalidOperationException("You cannot assign two outputs of the same amount to the same address in the same transaction.");

            Inputs = inputs.OrderBy(i => i.PreviousTransactionId).ThenBy(i => i.PreviousOutputIndex).ToList();
            Outputs = outputs.OrderBy(o => Convert.ToHexString(o.Address)).ThenBy(o => o.Amount).ToList();
            BlockNumber = blockNumber;

            TransactionId = GetTxIdAsString();
        }

        public string GetTxIdAsString()
        {
            return Convert.ToHexString(
                SHA256.Create().ComputeHash(new TransactionStruct
                {
                    Version = Version,
                    Inputs = Inputs,
                    Outputs = Outputs
                })
            );
        }
    }
}
