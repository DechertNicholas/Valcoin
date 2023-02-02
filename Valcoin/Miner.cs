using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Valcoin.Models;
using Valcoin.Services;

namespace Valcoin
{
    public static class Miner
    {
        internal static bool Stop = false;
        private static int Difficulty = 22;
        private static byte[] DifficultyMask = new byte[32];
        private static bool Initialized = false;


        public static void Initialize()
        {
            StorageService.SetupDB();
            // SynchronizeChain();
            Initialized = true;
        }

        public static void Mine()
        {
            if (!Initialized) { Initialize(); }

            // how many 0 bits need to lead the SHA256 hash. 256 is max, which would be impossible.
            // a difficulty of 6 means the has must be "000000xxxxxx..."
            SetDifficultyMask(Difficulty); // TODO: get this from the network


#if DEBUG
            // useful for determining starting difficulty by using time-to-mine
            var watch = new System.Diagnostics.Stopwatch();
            watch.Start();
#endif
            while (Stop == false)
            {
                var hashFound = false;
                var lastBlock = StorageService.GetLastBlock();
                var currentBlock = new ValcoinBlock()
                {
                    BlockId = lastBlock.BlockId + 1,
                    PreviousBlockHash = lastBlock.BlockHash,
                    BlockDifficulty = Difficulty
                };
                // TODO: select transactions, condense the root

                while (!hashFound)
                {
                    currentBlock.BlockHash = currentBlock.ComputeHash();
                    for (int i = 0; i < DifficultyMask.Length; i++)
                    {
                        if (currentBlock.BlockHash[i] > DifficultyMask[i])
                        {
                            // didn't get the hash, try new nonce
                            currentBlock.Nonce++;
                            break;
                        }
                        else if (i == DifficultyMask.Length - 1)
                        {
#if DEBUG
                            watch.Stop();
#endif
                            //Stop = true;
                            StorageService.Add(currentBlock);

                            var str = Convert.ToHexString(currentBlock.BlockHash);
                            Console.WriteLine(str);
                            hashFound = true;
                        }
                    }
                }
            }
        }

        public static void SetDifficultyMask(int difficulty)
        {
            int bytesToShift = Convert.ToInt32(Math.Ceiling(difficulty / 8d)); // 8 bits in a byte

            var difficultyMask = new byte[Convert.ToInt32(bytesToShift)];

            // fill 0s
            // bytesToShift - 1, because we don't want to fill the last byte
            for (var i = 0; i < bytesToShift - 1; i++)
            {
                difficultyMask[i] = 0x00;
            }

            int toShift = difficulty - (8 * (bytesToShift - 1));
            difficultyMask[^1] = (byte)(0b_1111_1111 >> toShift);
            DifficultyMask = difficultyMask;
        }
    }
}