using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using Valcoin.Models;

namespace Valcoin.Services
{
    public class ValcoinContext : DbContext
    {
        public virtual DbSet<ValcoinBlock> ValcoinBlocks { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<TxInput> TxInputs { get; set; }
        public DbSet<TxOutput> TxOutputs { get; set; }
        public DbSet<Wallet> Wallets { get; set; }
        public DbSet<Client> Clients { get; set; }

        private static bool dbRefreshed;

        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite($"Filename=valcoin.db");

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<ValcoinBlock>().Navigation(b => b.Transactions).AutoInclude();
            modelBuilder.Entity<Transaction>().Navigation(t => t.Inputs).AutoInclude();
            modelBuilder.Entity<Transaction>().Navigation(t => t.Outputs).AutoInclude();
        }

        /// <summary>
        /// Static, run-once constructor to initialize the databases and never attempt to re-initialize.
        /// </summary>
        static ValcoinContext()
        {
            var context = new ValcoinContext();
#if !DEBUG___PERSIST_DB && !RELEASE
            // contexts get re-created, and we need to ensure we don't keep deleting the DB
            if (!dbRefreshed)
            {
                // delete and remake in debug env
                context.Database.EnsureDeleted();
                dbRefreshed = true;
            }
#endif
            // create the database
            context.Database.EnsureCreated();
        }
    }
}