using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Valcoin.Core
{
    internal static class Miner
    {
        public static bool Stop = false;

        public static void Mine()
        {
            // how many 0's need to lead the SHA256 hash. 64 is max, which would be impossible.
            // a difficulty of 6 means the has must be "000000xxxxxx..."
            var difficulty = 6;
            var difficultyMask = new string('0', difficulty);

            var Hasher = SHA256.Create();
            var currentBlock = BuildTestBlock(); // This will eventually be rewritten for node comms
            var hash = new string('1', 64);

            while (Stop == false && hash[..difficulty] != difficultyMask)
            {
                // compute hash
            }
        }

        private static ValcoinBlock BuildTestBlock()
        {
            var block = new ValcoinBlock();
            block.BlockHeader.PreviousHash = new string('0', 64);
            return block;
        }
    }
}
