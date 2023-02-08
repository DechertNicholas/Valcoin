using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Valcoin.Models
{
    public class ValcoinBlock
    {
        /// <summary>
        /// The number of the block in the blockchain sequence.
        /// </summary>
        [Required]
        [Key]
        public ulong BlockId { get; set; } = 0;

        /// <summary>
        /// The hash of the current block data that is in the block header.
        /// </summary>
        public byte[] BlockHash { get; set; } = new byte[32];

        /// <summary>
        /// Hash of the previous block.
        /// </summary>
        public byte[] PreviousBlockHash { get; set; } = new byte[32];

        /// <summary>
        /// An array of transactions for this block to process.
        /// </summary>
        [NotMapped]
        public List<Transaction> Transactions { get; set; }

        /// <summary>
        /// <see cref="Transactions"/> in JOSN format for database storage, as SQLite can only store primitive types.
        /// </summary>
        public string JsonTransactions { get; set; }

        /// <summary>
        /// The random value assigned to the block header for changing the hash. Critical for proof-of-work.
        /// </summary>
        public ulong Nonce { get; set; } = 0;

        /// <summary>
        /// The time of the block being hashed.
        /// </summary>
        public DateTime TimeUTC { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// The difficulty on the blockchain at the time this block was hashed.
        /// </summary>
        public int BlockDifficulty { get; set; } = 0;

        /// <summary>
        /// The hash root of all the transactions in the block.
        /// </summary>
        public byte[] MerkleRoot { get; set; } = new byte[32];

        /// <summary>
        /// The version of this block, in case it ever changes.
        /// </summary>
        public int Version { get; set; } = 1;

        public ValcoinBlock() { }

        public ValcoinBlock(ulong blockId, byte[] previousBlockHash, ulong nonce, DateTime timeUTC, int blockDifficulty)
        {
            BlockId = blockId;
            PreviousBlockHash = previousBlockHash;
            Nonce = nonce;
            TimeUTC = timeUTC;
            BlockDifficulty = blockDifficulty;
        }

        public void AddTx(Transaction tx)
        {
            Transactions.Add(tx);
            JsonTransactions = JsonSerializer.Serialize(Transactions);
        }

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