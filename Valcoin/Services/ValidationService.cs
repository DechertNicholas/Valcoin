using Microsoft.UI.Xaml.Documents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Valcoin.Models;

namespace Valcoin.Services
{

    public static class ValidationService
    {
        public enum ValidationCode
        {
            Valid,
            Existing,         // we already have it
            Invalid,          // codes after "Invalid" are requests for more information. Currently, the transaction cannot be validated or invalidated
            Miss_Prev_Block
        }

        /// <summary>
        /// All blocks handed to the ValidationService which were unable to be validated (but are not invalid). Normally pending network operations.
        /// </summary>
        private static List<ValcoinBlock> pendingBlocks = new();

        public static async Task<ValidationCode> ValidateBlock(ValcoinBlock block, StorageService service)
        {
            // first, check if we already have this block to avoid extra work
            if (await service.GetBlock(block.BlockHashAsString) != null)
                return ValidationCode.Existing;

            // if we don't already have it, then validate and add it to the database.
            // it doesn't matter if it's part of the longest chain or not, because that new chain may become the longest chain eventually.
            // before the whole block can be validated however, each transaction must first be validated since one invalid transaction will
            // spoil the whole block.
            // and for each transaction to be validated, we need to ensure we have the whole chain up to this block
            if (await service.GetBlock(Convert.ToHexString(block.PreviousBlockHash)) == null &&
                block.BlockHashAsString != new string('0', 64) &&                               // filter out the genesis hash
                block.BlockNumber != 1)                                                         // filter out the genesis block when paired with the blockhash
            {
                // we don't have the previous block. Request it.
                pendingBlocks.Add(block);
                return ValidationCode.Miss_Prev_Block;
            }

            // validate the difficulty
            // Same as miner code, build the difficulty mask
            int bytesToShift = Convert.ToInt32(Math.Ceiling(block.BlockDifficulty / 8d)); // 8 bits in a byte

            var difficultyMask = new byte[Convert.ToInt32(bytesToShift)];

            // fill 0s
            // bytesToShift - 1, because we don't want to fill the last byte
            for (var i = 0; i < bytesToShift - 1; i++)
            {
                difficultyMask[i] = 0x00;
            }

            int toShift = block.BlockDifficulty - (8 * (bytesToShift - 1));
            difficultyMask[^1] = (byte)(0b_1111_1111 >> toShift);

            // now validate
            for (int i = 0; i < difficultyMask.Length; i++)
            {
                if (block.BlockHash[i] > difficultyMask[i])
                {
                    return ValidationCode.Invalid;
                }
            }


            // we have the previous block, so all other blocks before that would have been validated as well. Process the transactions
            var txResults = await ValidateBlockTxs(block.Transactions);
            if (txResults != ValidationCode.Valid)
                return txResults;


            // lastly, validate the hashes in the block
            var givenHash = block.BlockHash;
            var givenRoot = block.MerkleRoot;

            // recompute the hashes in the block and ensure they match with what we were given. Don't implicitly trust things you
            // find lying around on the network
            block.ComputeAndSetHash();
            block.ComputeAndSetMerkleRoot();
            if (!(givenHash == block.BlockHash && givenRoot == block.MerkleRoot))
            {
                return ValidationCode.Invalid;
            }

            // all passes
            return ValidationCode.Valid;
        }

        /// <summary>
        /// Validates a group of transactions in a block, and returns a bool indicating if all transactions were valid (and block may be valid) or if
        /// any were invalid and thus, the block is invalid.
        /// </summary>
        /// <param name="txs">The transactions in a block to validate.</param>
        /// <returns></returns>
        public static async Task<ValidationCode> ValidateBlockTxs(List<Transaction> txs)
        {
            var allValidated = ValidationCode.Valid; // start off as true, then if anything marks as false, it will remain false
            // validate the coinbase transaction
            if (txs[0].Inputs.Length == 1 &&
                txs[0].Inputs[0].PreviousTransactionId == new string('0', 64) &&
                txs[0].Inputs[0].PreviousOutputIndex == -1)
            {
                var coinbase = txs[0];
                // debugging serialization
                var unlockBytes = (byte[])new UnlockSignatureStruct(coinbase.BlockNumber, coinbase.Inputs[0].UnlockerPublicKey);
                var inputValid = Wallet.VerifyData(
                    unlockBytes,
                    coinbase.Inputs[0].UnlockSignature,
                    coinbase.Inputs[0].UnlockerPublicKey);

                var outputValid = coinbase.Outputs.Length == 1 && coinbase.Outputs[0].Amount == 50;
                var txValid = coinbase.TxId == coinbase.GetTxIdAsString();

                if (!(inputValid && outputValid && txValid))
                    allValidated = ValidationCode.Invalid;
            }

            if (txs.Count == 1)
                return allValidated; // this was the only transaction

            // validate the rest as non-coinbase transactions
            foreach (var tx in txs.Where(t => txs.IndexOf(t) != 0))
            {
                if (await ValidateTx(tx, new StorageService()) == ValidationCode.Invalid)
                    return allValidated = ValidationCode.Invalid; // the whole block is bad, exit
            }

            return allValidated;
        }

        public static async Task<ValidationCode> ValidateTx(Transaction tx, IStorageService service)
        {
            var inputSum = 0;

            // validate the hash first since it's simple
            if (tx.TxId != tx.GetTxIdAsString())
                return ValidationCode.Invalid;

            // validate the inputs
            foreach (var input in tx.Inputs)
            {
                // get the referenced transaction data
                var prevTx = await service.GetTx(input.PreviousTransactionId);
                var prevOutput = prevTx.Outputs[input.PreviousOutputIndex];

                // compute the hashes and signatures
                var p2pkhValid = SHA256.Create().ComputeHash(input.UnlockerPublicKey).SequenceEqual(prevOutput.LockSignature);
                var sigValid = Wallet.VerifyData(
                    new UnlockSignatureStruct(prevTx.BlockNumber, input.UnlockerPublicKey),
                    input.UnlockSignature,
                    input.UnlockerPublicKey);

                if (!(p2pkhValid && sigValid))
                    return ValidationCode.Invalid;

                // add all the input amounts up to ensure the total output amount is equal to this.
                // since each input comes from a different transaction and needs a db call to aquire,
                // it's less expensive to just add this up here.
                inputSum += prevOutput.Amount;
            }

            // ensure the transaction has spent correctly
            if (tx.Outputs.Sum(o => o.Amount) != inputSum)
                return ValidationCode.Invalid;

            return ValidationCode.Valid;
        }
    }
}
