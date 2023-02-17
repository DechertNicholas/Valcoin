using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Valcoin.Models;

namespace Valcoin.UnitTests
{
    public class SerializationTests
    {
        private readonly SHA256 h = SHA256.Create();

        /// <summary>
        /// A <see cref="TxInput"/> will never be sent across a network on it's own, but we need to ensure that the properties needed
        /// only for database operation are not considered when hashing the object, or when sending it over a network.
        /// </summary>
        [Fact]
        public void TxInputSerialization()
        {
            var input = new TxInput(new string('0', 64), -1, new byte[] { 1 }, new byte[] { 2 })
            {
                TransactionId = "test" // assign this as if it were read from the database. Should not be considered in a hash or network operation
            };
            var data = JsonDocument.Parse((byte[])input);
            var inputd = data.Deserialize<TxInput>();

            Assert.True(h.ComputeHash(input)
                .SequenceEqual(h.ComputeHash(inputd)));
        }

        /// <summary>
        /// A <see cref="TxOutput"/> will never be sent across a network on it's own, but we need to ensure that the properties needed
        /// only for database operation are not considered when hashing the object, or when sending it over a network.
        /// </summary>
        [Fact]
        public void TxOutputSerialization()
        {
            var output = new TxOutput("0", 50, new byte[] { 1 })
            {
                TransactionId = "test" // both TransactionId and Index should not be considered in a hash or network operation
            };
            var data = JsonDocument.Parse((byte[])output);
            var outputd = data.Deserialize<TxOutput>();

            Assert.True(h.ComputeHash(output)
                .SequenceEqual(h.ComputeHash(outputd)));
        }

        [Fact]
        public void TransactionSerialization()
        {
            var input = new TxInput(new string('0', 64), -1, new byte[] { 1 }, new byte[] { 2 })
            {
                TransactionId = "test" // assign this as if it were read from the database. Should not be considered in a hash or network operation
            };
            var output = new TxOutput("0", 50, new byte[] { 1 })
            {
                TransactionId = "test" // both TransactionId and Index should not be considered in a hash or network operation
            };
            var tx = new Transaction(1, new() { input }, new() { output });

            var data = JsonDocument.Parse((byte[])tx);
            var txd = data.Deserialize<Transaction>();

            Assert.True(h.ComputeHash(tx)
                .SequenceEqual(h.ComputeHash(txd)));
        }

        [Fact]
        public void ValcoinBlockSerialization()
        {
            var input = new TxInput(new string('0', 64), -1, new byte[] { 1 }, new byte[] { 2 })
            {
                TransactionId = "test" // assign this as if it were read from the database. Should not be considered in a hash or network operation
            };
            var output = new TxOutput("0", 50, new byte[] { 1 })
            {
                TransactionId = "test" // both TransactionId and Index should not be considered in a hash or network operation
            };
            var tx = new Transaction(1, new() { input }, new() { output });
            var block = new ValcoinBlock(1, new byte[] { 1 }, 11, DateTime.UtcNow, 22);
            block.AddTx(tx);
            block.ComputeAndSetHash();

            var data = JsonDocument.Parse((byte[])block);
            var blockd = data.Deserialize<ValcoinBlock>();

            Assert.True(h.ComputeHash(block)
                .SequenceEqual(h.ComputeHash(blockd)));

            // re-assert true after recomputing the hash
            blockd.ComputeAndSetHash();
            Assert.True(h.ComputeHash(block)
                .SequenceEqual(h.ComputeHash(blockd)));
        }
    }
}
