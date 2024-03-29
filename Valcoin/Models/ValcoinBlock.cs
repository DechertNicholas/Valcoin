﻿using Microsoft.EntityFrameworkCore;
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
using Windows.Devices.Bluetooth.Advertisement;

namespace Valcoin.Models
{
    public class ValcoinBlock
    {
        /// <summary>
        /// Key for the DB since it can't use a byte[] as the key. Hex string of the <see cref="BlockHash"/>.
        /// </summary>
        [Key]
        public string BlockId { get; set; }
        /// <summary>
        /// The number of the block in the blockchain sequence. Starts at 1. A block with index 0 is invalid.
        /// </summary>
        public long BlockNumber { get; set; } = 0;
        /// <summary>
        /// The hash of the current block data that is in the block header.
        /// </summary>
        public byte[] BlockHash { get; set; } = new byte[32];
        /// <summary>
        /// Hash of the previous block.
        /// </summary>
        public byte[] PreviousBlockHash { get; set; } = new byte[32];
        /// <summary>
        /// The next block in the longest chain.
        /// </summary>
#nullable enable
        public byte[]? NextBlockHash { get; set; } = new byte[32];
#nullable disable
        /// <summary>
        /// The random value assigned to the block header for changing the hash. Critical for proof-of-work.
        /// </summary>
        public ulong Nonce { get; set; } = 0;
        /// <summary>
        /// The time of the block being hashed.
        /// </summary>
        public long TimeUTCTicks { get; set; } = DateTime.UtcNow.Ticks;
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

        /// <summary>
        /// An array of transactions for this block to process.
        /// </summary>
        public List<Transaction> Transactions { get; set; } = new();

        public static implicit operator byte[](ValcoinBlock b) => JsonSerializer.SerializeToUtf8Bytes(b);

        /// <summary>
        /// Parameterless constructor. Creates a default block.
        /// </summary>
        public ValcoinBlock() { }

        /// <summary>
        /// Json constructor. For DB operations, not normal use.
        /// </summary>
        [JsonConstructor] // for serialization over the network
        public ValcoinBlock (string blockId, long blockNumber, byte[] blockHash, byte[] previousBlockHash,
            ulong nonce, long timeUTCTicks, int blockDifficulty, byte[] merkleRoot, List<Transaction> transactions)
        {
            BlockNumber = blockNumber;
            BlockHash = blockHash;
            BlockId = blockId;
            PreviousBlockHash = previousBlockHash;
            Transactions = transactions;
            Nonce = nonce;
            TimeUTCTicks = timeUTCTicks;
            BlockDifficulty = blockDifficulty;
            MerkleRoot = merkleRoot;
        }

        /// <summary>
        /// The standard constructor. Fills out required info to make an empty new block.
        /// </summary>
        /// <param name="blockNumber">The number in the chain this block will be.</param>
        /// <param name="previousBlockHash">The previous block's hash.</param>
        /// <param name="nonce">A random value to change when mining.</param>
        /// <param name="timeUTCTicks">The current time in UTC ticks.</param>
        /// <param name="blockDifficulty">The difficulty at the time of creation.</param>
        public ValcoinBlock(long blockNumber, byte[] previousBlockHash, ulong nonce, long timeUTCTicks, int blockDifficulty)
        {
            BlockNumber = blockNumber;
            PreviousBlockHash = previousBlockHash;
            Nonce = nonce;
            TimeUTCTicks = timeUTCTicks;
            BlockDifficulty = blockDifficulty;
        }

        /// <summary>
        /// Add a transaction to this block.
        /// </summary>
        /// <param name="tx">The transaction to add.</param>
        public void AddTx(Transaction tx)
        {
            tx.ComputeAndSetTransactionId();
            Transactions.Add(tx);
            ComputeAndSetMerkleRoot();
        }

        /// <summary>
        /// Add multiple transactions at once to the block.
        /// </summary>
        /// <param name="txs">An enumerable of transactions to add.</param>
        public void AddTx(IEnumerable<Transaction> txs)
        {
            foreach (Transaction tx in txs)
            {
                Transactions.Add(tx);
            }
            ComputeAndSetMerkleRoot();
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
                TimeUTCTicks = TimeUTCTicks,
                BlockDifficulty = BlockDifficulty,
                MerkleRoot = MerkleRoot,
                Version = Version
            });

            BlockId = Convert.ToHexString(BlockHash);
        }

        /// <summary>
        /// Compute the Merkle Root (hash tree) of all transactions in the block.
        /// </summary>
        public void ComputeAndSetMerkleRoot()
        {
            // first, we sort the transactions. This preserves the order for hashing.
            // this is needed because when a block is loaded with transactions and inputs and outputs from the database,
            // EFCore adds those items to their respective collections in an uncontrolled order, resulting in a different
            // root hash
            List<Transaction> txs = Transactions.OrderBy(t => t.TransactionId).ToList();

            // it was very difficult to do this elegantly and without introducing new functions.
            // to make this easier, I've referenced the original bitcoin code for making the merkle root.
            // to keep the algorithm simple, if there are an odd number of transactions, the last one is duplicated ONLY for computing the root
            // https://github.com/bitcoin/bitcoin/blob/4405b78d6059e536c36974088a8ed4d9f0f29898/main.h#L880

            var h = SHA256.Create();
            var merkleTree = new List<byte[]>();

            // line up hashes of all transactions in a list
            foreach (var tx in txs)
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

            for (int nSize = txs.Count; nSize > 1; nSize = (nSize + 1) / 2)
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