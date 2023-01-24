using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Valcoin.Services;
using Windows.Media.Devices;

[assembly: InternalsVisibleTo("Valcoin.Tests")]
namespace Valcoin.Core
{
    internal static class Miner
    {
        public static bool Stop = false;
        private static byte[] DifficultyMask;
        private static readonly ValcoinContext _db = new();
        private static bool Initialized = false;

        public static void Initialize()
        {
            SetupDB();
            // SynchronizeChain();
            Mine();
        }

        public static void Mine()
        {
            if (!Initialized) { Initialize(); }

            // how many 0 bits need to lead the SHA256 hash. 256 is max, which would be impossible.
            // a difficulty of 6 means the has must be "000000xxxxxx..."
            SetDifficultyMask(10); // TODO: get this from the network

            var Hasher = SHA256.Create();
            var currentBlock = BuildGenesisBlock(); // This will eventually be rewritten for node comms

#if DEBUG
            // useful for determining starting difficulty by using time-to-mine
            var watch = new System.Diagnostics.Stopwatch();
            watch.Start();
#endif
            while (Stop == false)
            {
                var hash = Hasher.ComputeHash(currentBlock);
                for (int i = 0; i < DifficultyMask.Length; i++)
                {
                    if (DifficultyMask[i] != hash[i])
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
                        Stop = true;
                        _db.Add(currentBlock);
                    }
                }

                Console.WriteLine(Convert.ToHexString(hash));
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

        private static ValcoinBlock BuildGenesisBlock()
        {
            var block = new ValcoinBlock();
            var genesisHash = new byte[32];
            for (var i = 0; i < genesisHash.Length; i++)
            {
                genesisHash[i] = 0x00;
            }
            block.PreviousHash = genesisHash;
            block.BlockNumber = 0;
            return block;
        }

        private static void SetupDB()
        {
#if DEBUG
            // delete and remake in debug env
            //_db.Database.EnsureDeleted();
#endif
            // create the database
            _db.Database.EnsureCreated();
        }
    }
}
