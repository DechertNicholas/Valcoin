using Valcoin.Services;

namespace Valcoin.IntegrationTests
{
    /// <summary>
    /// A shared fixture for all test classes that require database access through <see cref="ValcoinContext"/>.
    /// </summary>
    public class DatabaseFixture : IDisposable
    {
        /// <summary>
        /// The database context.
        /// </summary>
        public ValcoinContext DB { get; private set; }

        public DatabaseFixture()
        {
            DB = new();
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            DB.Dispose();
        }
    }
}
