using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;

namespace Valcoin_Core
{
    public sealed class Node
    {
        private static readonly object InstanceLock = new object();
        private static Node instance = null;

        private Node()
        {

        }

        // I don't think this class will ever be referened by two different threads,
        // but since multithreading is planned, it's better to be safe than sorry.
        public static Node GetInstance
        {
            get
            {
                if (instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (instance == null)
                        {
                            instance = new Node();
                        }
                    }
                }
                return instance;
            }
        }

        public void StartNode()
        {
            //var db = new DbHandler();
            //db.CreateDBConnection();
            var block = BuildNewBlock();
            Console.WriteLine(block); // Just for debug to inspect the block
        }

        private Block BuildNewBlock()
        {
            var transactions = GetTransactionsFromTxPool();
            // we need the whole tree, not just the root hash
            var hashedBlockData = BuildMerkleRoot(transactions);
            return new Block
            {
                BlockNumber = 1, //placeholder
                BlockVersion = 1,
                PreviousHash = "D7E88CEFB452B8B53B6EEE935735BF21B915D651630324D45187F1C73E4828A6", // placeholder
                BlockDateTime = DateTime.UtcNow,
                TargetDifficulty = "000000FF00000000000000000000000000000000000000000000000000000000",
                RootHash = Utils.HashByteToString(hashedBlockData.NodeSHA256Hash),
                //TxData = hashedBlockData
            }; // The block contains the root hash twice (RootHash and root of TxData). Will figure out how to remove this later
        }

        private List<Transaction> GetTransactionsFromTxPool()
        {
            // for now, this gets a random list of transactions for the next block
            // will be replaced with network data in the future
            var randomList = new List<Transaction>();
            var randomizer = new Random();
            var randomTxNumber = randomizer.Next(64);
            for (var i = 0; i < randomTxNumber; i++)
            {
                var newTx = new Transaction
                {
                    Amount = decimal.Parse($"{ randomizer.Next(100)}.{randomizer.Next(99999999)}") // 69.13374200
                };
                randomList.Add(newTx);
            }
            return randomList;
        }

        private HashTreeNode BuildMerkleRoot(List<Transaction> transactions)
        {
            if (transactions.Count == 0)
            {
                // no transactions during this period, return empty string
                return new HashTreeNode
                {
                    NodeSHA256Hash = Utils.StringToByteArray(string.Empty)
                };
            }
            var SHA256Hasher = SHA256.Create();
            var rootBuilderList = new List<HashTreeNode>(); // will be a list of transaction hash nodes, then move up to root node

            foreach (var transaction in transactions)
            {
                var newLeaf = new HashTreeNode
                {
                    NodeSHA256Hash = SHA256Hasher.ComputeHash(transaction),
                    Tx = transaction
                };
                rootBuilderList.Add(newLeaf);
            }

            var hashConcat = new StringBuilder();
            // we recursively replace our rootBuilderList with a new, shorter list
            // each run moves a list up a level in the tree, until a root node is found
            while (rootBuilderList.Count != 1)
            {
                if (rootBuilderList.Count % 2 != 0)
                {
                    // odd number of transactions, copy the last one
                    // the duplicate transaction will already be processed by the node, and discarded
                    // since the timestamp and signature will be the same.
                    // This applies to leaves as well (26 / 2 = 13, need even number of leaves)
                    rootBuilderList.Add(rootBuilderList[^1]);
                }
                var nextLevelList = new List<HashTreeNode>(); // init a new list each time, so we want it to be remade
                for (var i = 0; i < rootBuilderList.Count; i += 2)
                {
                    var newNode = new HashTreeNode
                    {
                        Left = rootBuilderList[i],
                        Right = rootBuilderList[i + 1]
                    };
                    hashConcat.Append(Utils.HashByteToString(rootBuilderList[i].NodeSHA256Hash));
                    hashConcat.Append(Utils.HashByteToString(rootBuilderList[i + 1].NodeSHA256Hash));
                    newNode.NodeSHA256Hash = SHA256Hasher.ComputeHash(
                        Utils.StringToByteArray(hashConcat.ToString())
                    );
                    nextLevelList.Add(newNode);
                    hashConcat.Clear(); // reset, otherwise we'll have a veeery long hash string
                }
                // nextLevelList contains new "higher order" leaves, which each contain two hashes below them
                // which eventually link to a transaction. We replace our rootBuilderList with this new nextLevelList,
                // and then feed the new data back into the loop to generate new parent nodes which recursively contain
                // hashes that eventually lead to transactions
                rootBuilderList = nextLevelList;
            }

            return rootBuilderList[0]; // when there is only one element left, return it as the root node
        }
    }
}
