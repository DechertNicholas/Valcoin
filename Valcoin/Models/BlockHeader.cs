using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Valcoin.Models
{
    /// <summary>
    /// Helper struct for <see cref="Block"/>, holding the data which will be hashed without needing to store it outside the <see cref="Block"/>.
    /// </summary>
    public struct BlockHeader
    {
        public byte[] PreviousBlockHash { get; set; }
        public ulong Nonce { get; set; }
        public DateTime TimeUTC { get; set; }
        public int BlockDifficulty { get; set; }
        public byte[] MerkleRoot { get; set; }
        public int Version { get; set; }

        public static implicit operator byte[](BlockHeader b) => JsonSerializer.SerializeToUtf8Bytes(b);
    }
}