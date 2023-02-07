using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Valcoin.Exceptions.TransactionExceptions;

namespace Valcoin.Models
{
    public class Transaction
    {
        //public int TransactionId { get; private set; }

        /// <summary>
        /// The version of transaction data formatting being used.
        /// </summary>
        public int Version { get; set; } = 1;

        /// <summary>
        /// The hash of the transaction in hex string format.
        /// </summary>
        [Key]
        public string TxId { get; private set; }

        /// <summary>
        /// JSON representations of <see cref="Inputs"/>. Entity Framework Core can only store primitive types, so a JSON string will let us store a string
        /// while also allowing us to populate <see cref="Inputs"/> with the data from that string.
        /// </summary>
        public string JsonInputs { get; private set; }

        /// <summary>
        /// <see cref="TxInput"/>s used for this transaction. Can include any number of <see cref="TxInput"/>s so long as the resulting value of all 
        /// <see cref="TxInput"/>s is greater than or equal to the <see cref="TxOutput"/>s for this transaction.
        /// </summary>
        [NotMapped]
        public TxInput[] Inputs { get; private set; }

        /// <summary>
        /// JSON representations of <see cref="Outputs"/>. Entity Framework Core can only store primitive types, so a JSON string will let us store a string
        /// while also allowing us to populate <see cref="Outputs"/> with the data from that string.
        /// </summary>
        public string JsonOutputs { get; private set; }

        /// <summary>
        /// The outputs of this transaction. Max 2 - the output sent to the recipient, and any change that goes back to the sender.
        /// </summary>
        [NotMapped]
        public TxOutput[] Outputs { get; private set; }

        //public Transaction() { }

        public Transaction(TxInput[] inputs, TxOutput[] outputs)
        {
            Inputs = inputs;
            JsonInputs = JsonSerializer.Serialize(Inputs);
            Outputs = outputs;
            JsonOutputs = JsonSerializer.Serialize(Outputs);
            TxId = GetTxIdAsString();
        }

        public Transaction(int version, string txId, string jsonInputs, string jsonOutputs)
        {
            Version = version;
            TxId = txId;
            Inputs = JsonSerializer.Deserialize<TxInput[]>(jsonInputs);
            JsonInputs = JsonSerializer.Serialize(Inputs);
            Outputs = JsonSerializer.Deserialize<TxOutput[]>(jsonOutputs);
            JsonOutputs = JsonSerializer.Serialize(Outputs);
        }

        public string GetTxIdAsString()
        {
            return Convert.ToHexString(
                SHA256.Create().ComputeHash(new TransactionStruct
                {
                    Inputs = Inputs,
                    Outputs = Outputs
                })
            );
        }
    }
}
