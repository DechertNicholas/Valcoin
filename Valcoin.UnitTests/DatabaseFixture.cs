using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Valcoin.Services;

namespace Valcoin.UnitTests
{
    public class DatabaseFixture : IDisposable
    {
        /// <summary>
        /// The database context.
        /// </summary>
        public ValcoinContext Context { get; private set; } = new();

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Context.Dispose();
        }
    }
}
