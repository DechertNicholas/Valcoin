using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Valcoin.Exceptions.TransactionExceptions;

[assembly: InternalsVisibleTo("Valcoin.UnitTests")]
namespace Valcoin.Models
{
    internal class Transaction
    {
        /// <summary>
        /// The version of transaction data formatting being used.
        /// </summary>
        public int Version { get; set; } = 1;

        public string TxId
        {
            get => GetTxIdAsString();
        }

        /// <summary>
        /// JSON representations of <see cref="Inputs"/>. Entity Framework Core can only store primitive types, so a JSON string will let us store a string
        /// while also allowing us to populate <see cref="Inputs"/> with the data from that string.
        /// </summary>
        public string InputsJson { get; set; }

        /// <summary>
        /// <see cref="TxInput"/>s used for this transaction. Can include any number of <see cref="TxInput"/>s so long as the resulting value of all 
        /// <see cref="TxInput"/>s is greater than or equal to the <see cref="TxOutput"/>s for this transaction.
        /// </summary>
        [NotMapped]
        public TxInput[] Inputs { get; set; }

        /// <summary>
        /// JSON representations of <see cref="Outputs"/>. Entity Framework Core can only store primitive types, so a JSON string will let us store a string
        /// while also allowing us to populate <see cref="Outputs"/> with the data from that string.
        /// </summary>
        public string OutputsJson { get; set; }

        /// <summary>
        /// The outputs of this transaction. Max 2 - the output sent to the recipient, and any change that goes back to the sender.
        /// </summary>
        [NotMapped]
        public TxOutput[] Outputs { get; set; }

        public Transaction() { }

        public Transaction(int amount, string receiverAddress, TxInput[] inputs)
        {
            if (receiverAddress[..2] != "0x" && receiverAddress.Length != 34)
            {
                throw new InvalidRecipientAddressException($"The address {receiverAddress} is not valid. It must start with '0x' and be 66 characters in length.");
            }

            Inputs = inputs;

            // verify the amount attempting to be sent
            var totalInputValue = Inputs.Sum(i => i.);
            if (totalInputValue < amount)
                throw new InvalidSendingAmountException($"The sum of all inputs ({totalInputValue}) is less than the amount attempting to be sent ({amount}).");

            // build the outputs for this transaction
            var receiverOutput = new TxOutput
            {
                Amount = amount,
                LockSignature = Convert.FromHexString(receiverAddress)
            };

            if (totalInputValue > amount)
            {
                var changeAmount = new TxOutput
                {
                    Amount = totalInputValue - amount,
                    LockSignature = Inputs[0].PreviousOutput.LockSignature
                };
            }
        }

        public string GetTxIdAsString()
        {
            return Convert.ToHexString(
                SHA256.Create().ComputeHash(new TransactionStruct
                    {
                        Inputs = Inputs,
                        Outputs = Outputs
                    }
                )
            );
        }
    }
}
