using Microsoft.EntityFrameworkCore;
using Valcoin.Models;

namespace Valcoin.Services
{
    internal class ValcoinContext : DbContext
    {
        public DbSet<ValcoinBlock> ValcoinBlocks { get; set; }
        public DbSet<Wallet> Wallets { get; set; }
        public DbSet<Transaction> Transactions { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite($"Filename=valcoin.db");

        public ValcoinContext()
        {
#if !DEBUG___PERSIST_DB && !RELEASE
            // delete and remake in debug env
            this.Database.EnsureDeleted();
#endif
            // create the database
            this.Database.EnsureCreated();
        }
    }
}