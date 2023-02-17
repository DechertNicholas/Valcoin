using Microsoft.EntityFrameworkCore;
using Valcoin.Models;

namespace Valcoin.Services
{
    public class ValcoinContext : DbContext
    {
        public DbSet<ValcoinBlock> ValcoinBlocks { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<TxInput> TxInputs { get; set; }
        public DbSet<TxOutput> TxOutputs { get; set; }
        public DbSet<Wallet> Wallets { get; set; }
        public DbSet<Client> Clients { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite($"Filename=valcoin.db");

        /// <summary>
        /// Static, run-once constructor to initialize the databases and never attempt to re-initialize.
        /// </summary>
        static ValcoinContext()
        {
            var context = new ValcoinContext();
#if !DEBUG___PERSIST_DB && !RELEASE
            // delete and remake in debug env
            context.Database.EnsureDeleted();
#endif
            // create the database
            context.Database.EnsureCreated();
        }
    }
}