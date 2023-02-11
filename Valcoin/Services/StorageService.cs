using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Valcoin.Models;

namespace Valcoin.Services
{
    internal static class StorageService
    {
        public static ValcoinContext Db { get; private set; } = new ValcoinContext();

        public static ValcoinBlock GetLastBlock()
        {
            uint? lastId = Db.ValcoinBlocks.Max(b => (uint?)b.BlockNumber);
            return Db.ValcoinBlocks.FirstOrDefault(b => b.BlockNumber == lastId);
        }

        public static void AddBlock(ValcoinBlock block)
        {
            Db.Add(block);
            Db.SaveChanges();
        }

        public static void AddTxs(IEnumerable<Transaction> txs)
        {
            foreach (Transaction tx in txs)
            {
                Db.Add(tx);
            }
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
