using Microsoft.EntityFrameworkCore;
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
    internal class ValcoinBlock
    {
        /// <summary>
        /// The number of the block in the blockchain sequence.
        /// </summary>
        [Required]
        [Key]
        public ulong BlockId { get; set; } = 0;
        /// <summary>
        /// The hash of the current block. Assigned once the nonce which results in a valid hash is found.
        /// </summary>
        public byte[] BlockHash { get; set; } = new byte[32];
        /// <summary>
        /// Header for the block, which is what actually gets hashed. 
        /// </summary>

        public byte[] PreviousBlockHash { get; set; } = new byte[32];

        public ulong Nonce { get; set; } = 0;

        public DateTime TimeUTC { get; set; } = DateTime.UtcNow;

        public int BlockDifficulty { get; set; } = 0; // for knowing the difficulty at the time this block was hashed

        public byte[] MerkleRoot { get; set; } = new byte[32]; // blocks can be processed with no transactions, and thus no root

        public int Version { get; set; } = 1;

        /// <summary>
        /// Computes the hash for the current block.
        /// </summary>
        /// <returns>The hash of the current <see cref="BlockHeader"/>.</returns>
        public byte[] ComputeHash()
        {
            return SHA256.Create().ComputeHash(new BlockHeader
            {
                PreviousBlockHash = PreviousBlockHash,
                Nonce = Nonce,
                TimeUTC = TimeUTC,
                BlockDifficulty = BlockDifficulty,
                MerkleRoot = MerkleRoot,
                Version = Version
            });
        }
    }
}