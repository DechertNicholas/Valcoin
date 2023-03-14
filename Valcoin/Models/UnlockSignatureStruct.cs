using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Valcoin.Models
{
    /// <summary>
    /// A helper struct used to get the byte[] data of the UnlockSignature on a TxInput.
    /// </summary>
    public struct UnlockSignatureStruct
    {
        /// <summary>
        /// The transaction we are computing TxInput UnlockSignatures for.
        /// </summary>
        public Transaction Transaction { get; set; }

        /// <summary>
        /// The length of the byte[] used as the override on the transaction input's UnlockSignature. We cannot know the signature
        /// before it is computed, to it is instead computed with this value and overwritten with the result.
        /// </summary>
        private const int unlockOverrideLength = 4;

        public static implicit operator byte[](UnlockSignatureStruct u) => GetBytes(u.Transaction);

        public UnlockSignatureStruct(Transaction tx)
        {
            Transaction = tx;
        }

        /// <summary>
        /// Gets the byte[] data for the unlock signature, to be passed to the wallet to Sign.
        /// </summary>
        /// <param name="tx"></param>
        /// <returns></returns>
        private static byte[] GetBytes(Transaction tx)
        {
            var unlockOverride = new byte[unlockOverrideLength];
            byte[] returnBytes = Array.Empty<byte>();

            foreach (var input in tx.Inputs)
            {
                returnBytes = returnBytes.Concat(Convert.FromHexString(input.PreviousTransactionId)).ToArray();
                returnBytes = returnBytes.Concat(BitConverter.GetBytes(input.PreviousOutputIndex)).ToArray();
                returnBytes = returnBytes.Concat(input.UnlockerPublicKey).ToArray();

                // check if this is a coinbase transaction
                if (!input.PreviousTransactionId.Any(c => c != '0'))
                {
                    // use the block number to make it unique
                    returnBytes = returnBytes.Concat(BitConverter.GetBytes(tx.BlockNumber)).ToArray();
                }
                else
                {
                    returnBytes = returnBytes.Concat(unlockOverride).ToArray();
                }
            }

            foreach (var output in tx.Outputs)
            {
                returnBytes = returnBytes.Concat(BitConverter.GetBytes(output.Amount)).ToArray();
                returnBytes = returnBytes.Concat(output.Address).ToArray();
            }

            return returnBytes;
        }
    }
}
