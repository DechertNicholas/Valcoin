using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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
        public List<Transaction> Transactions { get; set; } = new();

        /// <summary>
        /// <see cref="Transactions"/> in JOSN format for database storage, as SQLite can only store primitive types.
        /// </summary>
        [JsonIgnore]
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
            ComputeMerkleRoot();
        }

        public void AddTx(IEnumerable<Transaction> txs)
        {
            foreach (Transaction tx in txs)
            {
                Transactions.Add(tx);
            }
            JsonTransactions = JsonSerializer.Serialize(Transactions);
            ComputeMerkleRoot();
        }

        /// <summary>
        /// Computes the hash for the current block.
        /// </summary>
        /// <returns>The hash of the current <see cref="BlockHeader"/>.</returns>
        public void ComputeAndSetHash()
        {
            BlockHash = SHA256.Create().ComputeHash(new BlockHeader
            {
                PreviousBlockHash = PreviousBlockHash,
                Nonce = Nonce,
                TimeUTC = TimeUTC,
                BlockDifficulty = BlockDifficulty,
                MerkleRoot = MerkleRoot,
                Version = Version
            });
        }

        
        public void ComputeMerkleRoot()
        {
            // it was very difficult to do this elegantly and without introducing new functions.
            // to make this easier, I've referenced the original bitcoin code for making the merkle root.
            // to keep the algorithm simple, if there are an odd number of transactions, the last one is duplicated ONLY for computing the root
            // https://github.com/bitcoin/bitcoin/blob/4405b78d6059e536c36974088a8ed4d9f0f29898/main.h#L880

            var h = SHA256.Create();
            var merkleTree = new List<byte[]>();

            // line up hashes of all transactions in a list
            foreach (var tx in Transactions)
                merkleTree.Add(h.ComputeHash(tx));

            /*
             * j is the "level" of the tree we are in.
             * new "levels" of the tree are added to the end of the list.
             * for index purposes, it is a multiplier of sorts for levels - as new hashes are added to the list,
             * j moves to mark the end of the last "level" and the beginning of the next "level".
             * 
             *  /// level 0 ///
             * Transactions = t0, t1, t2, t3, t4, t5
             * merkleTree   = h0, h1, h2, h3, h4, h5
             *                ^j = 0
             *                
             *  compute hash(h0+h1) = h01, etc.
             *  /// level 1 ///                      |
             * merkleTree   = h0, h1, h2, h3, h4, h5,| h01, h23, h45
             *                                       | ^j = 6 (index 6)
             *                 h01 = hash of h0 and h1 ^
             *                
             *  /// level 2 ///                      |               |
             *  merkleTree  = h0, h1, h2, h3, h4, h5,| h01, h23, h45,| h03, h45* <- (odd number of groups, h45 'prime' is h45 hashed with itself: hash(h45+h45) )
             *                                       |               | ^ j = 9
             *                               h03 = hash of h01 and h23 ^
             *                               
             *  /// level 3 ///                      |               |           |
             *  merkleTree  = h0, h1, h2, h3, h4, h5,| h01, h23, h45,| h03, h45*,| h05
             *                                       |               |           | ^ j = 11
             *                                          h05 = hash of h03 and h45* ^
             *  h05 is the hash root of all child hashes in the tree, and is the merkle root.
             *  
             *  in case of an odd number of transactions, the "odd one out" is simply hashed twice and added to the end:
             *  
             *  level 4              hx0123__4444   <- merkle root
             *                      /            \
             *  level 3      hx01_23              hx44_44
             *              /       \            /       \
             *  level 2     hx01      hx23       hx44  -> hx44
             *             /    \    /    \     /    \
             *  level 1    hx0   hx1 hx2   hx3  hx4-> hx4
             *              |     |   |     |    |
             *             tx0   tx1 tx2   tx3  tx4
             */
            int j = 0;

            for (int nSize = Transactions.Count; nSize > 1; nSize = (nSize + 1) / 2)
            {
                for (int i = 0; i < nSize; i += 2)
                {
                    int i2 = Math.Min(i + 1, nSize - 1);
                    merkleTree.Add(h.ComputeHash(( merkleTree[j + i] )
                        .Concat( merkleTree[j + i2] )
                        .ToArray()));
                }
                j += nSize;
            }
            MerkleRoot = merkleTree.LastOrDefault();
        }
    }
}