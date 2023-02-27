using Microsoft.EntityFrameworkCore;
using Valcoin.Models;

namespace Valcoin.Services
{
    // This interface exists purely to allow mocking of the ValcoinContext. Does nothing otherwise
    public interface IContext
    {
        public DbSet<ValcoinBlock> ValcoinBlocks { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<TxInput> TxInputs { get; set; }
        public DbSet<TxOutput> TxOutputs { get; set; }
        public DbSet<Wallet> Wallets { get; set; }
        public DbSet<Client> Clients { get; set; }
    }
}
