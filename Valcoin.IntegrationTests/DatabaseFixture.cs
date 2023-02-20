using System.Security.Cryptography;
using Valcoin.Models;
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
        public ValcoinContext Context { get; private set; }
        public Wallet Wallet { get; private set; }

        public DatabaseFixture()
        {
            Context = new();

            // Create the test wallet with static data
            // this is a random public/private keyset that was generated. Not in use.
            ECParameters ecParams = new()
            {
                Curve = ECCurve.NamedCurves.nistP256,
                D = Convert.FromHexString("490CC7A8A8FDC8E2196591B86AECFA6B138968E7D828906E80715A379044932F"),
                Q = new ECPoint()
                {
                    X = Convert.FromHexString("BED612DDD11CA8237AF64DEE0EF9B5605A7C487C97E457F117D23CD111BFB376"),
                    Y = Convert.FromHexString("DA9EF038E8A08898A219171226107ACCB77DA940EA40B5CF0295BF28B7A2C5F0")
                }
            };
            var ecdsa = ECDsa.Create(ecParams);
            Wallet = new Wallet(ecdsa.ExportSubjectPublicKeyInfo(), ecdsa.ExportECPrivateKey());
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Context.Dispose();
        }
    }
}
