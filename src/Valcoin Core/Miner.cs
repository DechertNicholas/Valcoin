using System.Linq;
using System.Security.Cryptography;
using System;
using System.Text;

namespace Valcoin_Core
{
    public class Miner
    {
        // We set these here so that the objects don't have to be rebuilt for every new hash attempt
        public SHA256 Hasher;
        public RandomNumberGenerator RandomNumberGen;

        // Mining entry point
        public void BeginMining()
        {
            var watch = new System.Diagnostics.Stopwatch();
            Hasher = SHA256.Create();
            Hasher.Initialize();
            RandomNumberGen = RandomNumberGenerator.Create();
            var currentBlock = BuildTestBlock(); // This will eventually be rewritten for node comms
            var difficulty = new byte[32];
            var hash = new byte[32];

            // fill the array
            for (var i = 0; i < 32; i++)
            {
                difficulty[i] = 0x00;
            }

            // set our test values
            difficulty[3] = 0xFF;

            // For now, we only mine one test block
            var roundsCompleted = 0;
            var tenSeconds = new TimeSpan(0, 0, 10);
            var hashFound = false;
            watch.Start();
            while (!hashFound)
            {
                Hasher.Initialize(); // reset state each attempt
                ComputeBlockHash(Hasher, RandomNumberGen, currentBlock, difficulty, out hash, out hashFound);
                //Console.WriteLine($"Attempted Hash: {ByteArrayToString(hash)}");
                roundsCompleted++;
                if (watch.Elapsed > tenSeconds)
                {
                    watch.Stop();
                    Console.WriteLine($"{roundsCompleted / 10} hashes per second");
                    roundsCompleted = 0; // reset
                    watch.Restart();
                }
            }
            if (watch.IsRunning)
            {
                watch.Stop(); // safely stop
            }

            Console.WriteLine($"Difficulty: {ByteArrayToString(difficulty)}");
            Console.WriteLine($"Found Hash: {ByteArrayToString(hash)}");
        }

        // Assemble the current unmined block
        private Block BuildTestBlock()
        {
            var block = new Block();
            //{
            //    dateTime = DateTime.UtcNow,
            //    previousHash = "00DDDF648D22B590B1855413AB0F6AB576B11089B75E71922C5B3FAC040AEF53",
            //    txData = new byte[] { 0x01, 0x02, 0x05 } // dummy data
            //};
            return block;
        }

        private void ComputeBlockHash(SHA256 hasher, RandomNumberGenerator randomGen, Block block, byte[] difficulty, out byte[] hash, out bool hashFound)
        {
            hash = new byte[32];
            hashFound = false;
            randomGen.GetBytes(block.Nonce);
            var computedHash = hasher.ComputeHash(block);
            //if (computedHash[0] < 0x0F)
            //{
            //    Console.WriteLine("Found possible hash");
            //}

            for (var i = 0; i < 32; i++)
            {
                if (difficulty[i] == 0x00 && computedHash[i] != 0x00)
                {
                    // hash is higher value than the difficulty, invalid hash
                    // doesn't matter that we haven't found the difficulty setting yet
                    hashFound = false;
                    hash = computedHash;
                    break;
                }
                // here we finally find the difficulty setting
                // if the computed hash has been 0x00's up to here, this will determine if the has is valid
                else if (difficulty[i] != 0x00)
                {
                    if (computedHash[i] < difficulty[i])
                    {
                        hashFound = true;
                        hash = computedHash;
                        break;
                    }
                    else
                    {
                        hash = computedHash;
                    }
                }
            }
        }

        private static string ByteArrayToString(byte[] byteArray)
        {
            StringBuilder hexString = new StringBuilder(byteArray.Length * 2);
            foreach (byte singleByte in byteArray)
                hexString.AppendFormat("{0:x2}", singleByte);
            return hexString.ToString();
        }
    }
}
