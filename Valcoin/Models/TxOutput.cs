using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Valcoin.Models
{
    public struct TxOutput
    {
        /// <summary>
        /// The amount of Valcoin to send.
        /// </summary>
        public int Amount { get; set; }

        /// <summary>
        /// The address of the recipient - their hashed public key.
        /// </summary>
        public byte[] LockSignature { get; set; }
    }
}
