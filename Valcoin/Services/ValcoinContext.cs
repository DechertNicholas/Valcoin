using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Valcoin.Core;
using Windows.Storage;

namespace Valcoin.Services
{
    public class ValcoinContext : DbContext
    {
        public DbSet<ValcoinBlock> ValcoinBlocks { get; set; }

        public string DbPath { get; }

        public ValcoinContext()
        {
            DbPath = Path.Combine(
                ApplicationData.Current.LocalFolder.Path,
                "valcoin.db");
        }

        // The following configures EF to create a Sqlite database file in the
        // special "local" folder for your platform.
        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite($"Filename=valcoin.db");
    }
}
