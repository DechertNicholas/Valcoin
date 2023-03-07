using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Valcoin.Models
{
    public struct UnlockSignatureStruct
    {
        /// <summary>
        /// The length of the byte[] used as the override on the transaction input's UnlockSignature. We cannot know the signature
        /// before it is computed, to it is instead computed with this value and overwritten with the result.
        /// </summary>
        private const int unlockOverrideLength = 4;
        public Transaction Transaction { get; set; }

        public static implicit operator byte[](UnlockSignatureStruct u) => GetBytes(u.Transaction);

        public UnlockSignatureStruct(Transaction tx)
        {
            Transaction = tx;
        }

        private static byte[] GetBytes(Transaction tx)
        {
            var unlockOverride = new byte[unlockOverrideLength];
            byte[] returnBytes = Array.Empty<byte>();
            foreach (var input in tx.Inputs)
            {
                returnBytes = (byte[])returnBytes.Concat(Convert.FromHexString(input.PreviousTransactionId))
                    .Concat(BitConverter.GetBytes(input.PreviousOutputIndex))
                    .Concat(input.UnlockerPublicKey)
                    .Concat(unlockOverride)
                    .ToArray();
            }

            foreach (var output in tx.Outputs)
            {
                returnBytes = (byte[])returnBytes.Concat(BitConverter.GetBytes(output.Amount))
                    .Concat(output.Address)
                    .ToArray();
            }

            return returnBytes;
        }

        // TODO: we don't always know the block number for a tx, and cannot compute the unlock until then
        //public UnlockSignatureStruct(byte[] publicKey)
        //{
        //    // use byte copy to avoid referencing the same object
        //    PublicKey = new byte[publicKey.Length];
        //    publicKey.CopyTo(PublicKey, 0);
        //}

        //public UnlockSignatureStruct(ulong blockNumber, byte[] publicKey)
        //{
        //    this.BlockNumber = blockNumber;
        //    // use byte copy to avoid referencing the same object
        //    PublicKey = new byte[publicKey.Length];
        //    publicKey.CopyTo(PublicKey, 0);
        //}
    }
}
