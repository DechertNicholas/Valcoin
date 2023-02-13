using Microsoft.EntityFrameworkCore;
using Valcoin.Models;

namespace Valcoin.Services
{
    public class ValcoinContext : DbContext
    {
        public DbSet<ValcoinBlock> ValcoinBlocks { get; set; }
        public DbSet<Wallet> Wallets { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<Client> Clients { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            options.UseSqlite($"Filename=valcoin.db");
            options.EnableSensitiveDataLogging();
        }

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