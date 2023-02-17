using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Valcoin.Models
{
    public class Transaction
    {
        /// <summary>
        /// The hash of the transaction in hex string format.
        /// </summary>
        [Key]
        [JsonInclude]
        public string TransactionId { get; private set; }
        /// <summary>
        /// The version of transaction data formatting being used.
        /// </summary>
        public int Version { get; set; } = 1;
        /// <summary>
        /// JSON representations of <see cref="Inputs"/>. Entity Framework Core can only store primitive types, so a JSON string will let us store a string
        /// while also allowing us to populate <see cref="Inputs"/> with the data from that string.
        /// </summary>
        [JsonIgnore]
        public string JsonInputs { get; private set; }
        /// <summary>
        /// <see cref="TxInput"/>s used for this transaction. Can include any number of <see cref="TxInput"/>s so long as the resulting value of all 
        /// <see cref="TxInput"/>s is greater than or equal to the <see cref="TxOutput"/>s for this transaction.
        /// </summary>
        //[NotMapped]
        public List<TxInput> Inputs { get; private set; } = new();
        /// <summary>
        /// JSON representations of <see cref="Outputs"/>. Entity Framework Core can only store primitive types, so a JSON string will let us store a string
        /// while also allowing us to populate <see cref="Outputs"/> with the data from that string.
        /// </summary>
        [JsonIgnore]
        public string JsonOutputs { get; private set; }
        /// <summary>
        /// The outputs of this transaction. Max 2 - the output sent to the recipient, and any change that goes back to the sender.
        /// </summary>
        //[NotMapped]
        public List<TxOutput> Outputs { get; private set; } = new();
        /// <summary>
        /// The block in which this transaction was in. Part of the unlock signature.
        /// </summary>
        public ulong BlockNumber { get; set; }

        /// <summary>
        /// Used to link the transaction back to a block. Only part of DB operations.
        /// </summary>
        //public string BlockId { get; set; }

        /// <summary>
        /// Byte[] serializer used for transferring this transaction over the network.
        /// </summary>
        /// <param name="t"></param>
        public static implicit operator byte[](Transaction t) => JsonSerializer.SerializeToUtf8Bytes(t);

        /// <summary>
        /// The constructor used by other classes to build a new transaction.
        /// </summary>
        /// <param name="blockNumber">The BlockNumber this transaction is in. Used for DB relations.</param>
        /// <param name="inputs">The group of inputs for this transaction.</param>
        /// <param name="outputs">The group of outputs for this transaction.</param>
        [JsonConstructor]
        public Transaction(ulong blockNumber, List<TxInput> inputs, List<TxOutput> outputs)
        {
            Inputs = inputs;
            JsonInputs = JsonSerializer.Serialize(Inputs);
            Outputs = outputs;
            JsonOutputs = JsonSerializer.Serialize(Outputs);
            BlockNumber = blockNumber;
            TransactionId = GetTxIdAsString();
        }

        /// <summary>
        /// For loading from the database.
        /// </summary>
        public Transaction(ulong blockNumber, int version, string transactionId, string jsonInputs, string jsonOutputs)//, string blockId)
        {
            Version = version;
            TransactionId = transactionId;
            Inputs = JsonSerializer.Deserialize<List<TxInput>>(jsonInputs);
            JsonInputs = JsonSerializer.Serialize(Inputs);
            Outputs = JsonSerializer.Deserialize<List<TxOutput>>(jsonOutputs);
            JsonOutputs = JsonSerializer.Serialize(Outputs);
            BlockNumber = blockNumber;
            //BlockId = blockId;
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
