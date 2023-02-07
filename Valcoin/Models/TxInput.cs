using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Valcoin.Models
{
    public struct TxInput
    {
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
        /// Signed <see cref="TxOutput.LockSignature"/> of the locked output's <see cref="TxOutput.LockSignature"/>.
        /// </summary>
        public byte[] UnlockSignature { get; set; }
    }
}
