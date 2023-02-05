using System.Linq;
using Valcoin.Models;

namespace Valcoin.Services
{
    internal static class StorageService
    {
        public static ValcoinContext Db { get; private set; } = new ValcoinContext();

        public static ValcoinBlock GetLastBlock()
        {
            uint? lastId = Db.ValcoinBlocks.Max(b => (uint?)b.BlockId);
            if (lastId == null)
            {
                return BuildGenesisBlock();
            }
            return Db.ValcoinBlocks.First(b => b.BlockId == lastId);
        }

        public static ValcoinBlock BuildGenesisBlock()
        {
            var difficulty = 22; // starting difficulty
            var block = new ValcoinBlock();
            var genesisHash = new byte[32];
            for (var i = 0; i < genesisHash.Length; i++)
            {
                genesisHash[i] = 0x00; //TODO: hash this to something other than straight 0's
            }
            block.PreviousBlockHash = genesisHash;
            block.BlockId = 0;
            block.BlockDifficulty = difficulty;
            return block;
        }

        public static void AddBlock(ValcoinBlock block)
        {
            Db.Add(block);
            Db.SaveChanges();
        }

        public static void AddWallet(Wallet wallet)
        {
            Db.Add(wallet);
            Db.SaveChanges();
        }

        public static Wallet GetMyWallet()
        {
            return Db.Wallets.FirstOrDefault(w => w.PrivateKey != null);
        }
    }
}
