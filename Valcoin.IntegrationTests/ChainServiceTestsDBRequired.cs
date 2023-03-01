using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Valcoin.Models;
using Valcoin.Services;

namespace Valcoin.IntegrationTests
{
    [Collection(nameof(DatabaseCollection))]
    public class ChainServiceTestsDBRequired
    {
        readonly DatabaseFixture fixture;

        public ChainServiceTestsDBRequired(DatabaseFixture fixture)
        {
            this.fixture = fixture;
        }

        [Fact]
        public async void ReorganizeIsSuccessful()
        {
            // genesis block
            var block1 = new ValcoinBlock(1, new byte[] { 0 }, 1, DateTime.UtcNow.Ticks, 1);
            block1.ComputeAndSetMerkleRoot();
            block1.ComputeAndSetHash();

            // second block, will become new orphan
            var block2 = new ValcoinBlock(2, block1.BlockHash, 2, DateTime.UtcNow.Ticks, 1);
            block2.ComputeAndSetMerkleRoot();
            block2.ComputeAndSetHash();
            // link block1 to block2. This link will be broken during reorganize and pointed towards block2_2
            block1.NextBlockHash = block2.BlockHash;

            // the original orphan that will become the new main chain block2
            var block2_2 = new ValcoinBlock(2, block1.BlockHash, 22, DateTime.UtcNow.Ticks, 1);
            block2_2.ComputeAndSetMerkleRoot();
            block2_2.ComputeAndSetHash();

            // the block which comes in and beats our chain, causing a reorganization to take place
            var block3 = new ValcoinBlock(3, block2_2.BlockHash, 3, DateTime.UtcNow.Ticks, 1);
            block3.ComputeAndSetMerkleRoot();
            block3.ComputeAndSetHash();

            fixture.Context.Add(block1);
            fixture.Context.Add(block2);
            fixture.Context.Add(block2_2);
            await fixture.Context.SaveChangesAsync();

            // perform the reorganize (through the AddBlock method)
            var service = new ChainService(new ValcoinContext());
            await service.AddBlock(block3);

            var lastMainBlock = await service.GetLastMainChainBlock();

            Assert.True(block3.BlockHash.SequenceEqual(lastMainBlock.BlockHash));
            Assert.True(lastMainBlock.PreviousBlockHash.SequenceEqual(block2_2.BlockHash));
        }
    }
}
