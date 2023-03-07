using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Documents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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

        public static ValidationCode ValidateBlock(ValcoinBlock block)
        {
            var chainService = App.Current.Services.GetService<IChainService>();
            // first, check if we already have this block to avoid extra work
            if (chainService.GetBlock(block.BlockId).Result != null)
                return ValidationCode.Existing;

            // if we don't already have it, then validate and add it to the database.
            // it doesn't matter if it's part of the longest chain or not, because that new chain may become the longest chain eventually.
            // before the whole block can be validated however, each transaction must first be validated since one invalid transaction will
            // spoil the whole block.
            // and for each transaction to be validated, we need to ensure we have the whole chain up to this block
            if (chainService.GetBlock(Convert.ToHexString(block.PreviousBlockHash)).Result == null &&
                block.PreviousBlockHash != new byte[32] &&                            // filter out the genesis hash
                block.BlockNumber != 1)                                               // filter out the genesis block when paired with the blockhash
            {
                // we don't have the previous block. Request it.
                // we don't need to store this block either, as a sync request will get us up-to-date on the longest chain, including this block
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
            var txResults = ValidateBlockTxs(block.Transactions);
            if (txResults != ValidationCode.Valid)
                return txResults;


            // lastly, validate the hashes in the block
            var givenHash = new byte[block.BlockHash.Length];
            var givenRoot = new byte[block.MerkleRoot.Length];
            // use copy to avoid reference assigning (won't replicate changes)
            block.BlockHash.CopyTo(givenHash, 0);
            block.MerkleRoot.CopyTo(givenRoot, 0);

            // recompute the hashes in the block and ensure they match with what we were given. Don't implicitly trust things you
            // find lying around on the network
            block.ComputeAndSetHash();
            block.ComputeAndSetMerkleRoot();
            if (!(givenHash.SequenceEqual(block.BlockHash) && givenRoot.SequenceEqual(block.MerkleRoot)))
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
        public static ValidationCode ValidateBlockTxs(List<Transaction> txs)
        {
            var allValidated = ValidationCode.Valid; // start off as true, then if anything marks as false, it will remain false
            // validate the coinbase transaction
            // the coinbase tx is not always the first transaction, but it does always reference a blank previous tx with -1 as the input index
            var coinbases = txs.Where(t => t.Inputs[0].PreviousTransactionId == new string('0', 64))
                .Where(t => t.Inputs[0].PreviousOutputIndex == -1)
                .ToList();
            if (coinbases.Count > 1)
                return ValidationCode.Invalid; // more than one coinbase tx

            var coinbase = coinbases.First();

            if (coinbase != null)
            {
                var inputValid = Wallet.VerifyTransactionInputs(coinbase);

                var outputValid = coinbase.Outputs.Count == 1 && coinbase.Outputs[0].Amount == 50; // currently amount is statically set to 50
                var txValid = coinbase.TransactionId == coinbase.GetTxIdAsString();

                if (!(inputValid && outputValid && txValid))
                    allValidated = ValidationCode.Invalid;
            }

            if (txs.Count == 1)
                return allValidated; // this was the only transaction

            // validate the rest as non-coinbase transactions
            foreach (var tx in txs.Where(t => txs.IndexOf(t) != 0))
            {
                if (ValidateTx(tx) == ValidationCode.Invalid)
                    return allValidated = ValidationCode.Invalid; // the whole block is bad, exit
            }

            return allValidated;
        }

        /// <summary>
        /// Overload method to keep ease of use, but allow testability of code.
        /// </summary>
        /// <param name="tx">The transaction to validate</param>
        /// <returns>The <see cref="ValidationCode"/></returns>
        public static ValidationCode ValidateTx(Transaction tx)
        {
            return ValidateTx(tx, App.Current.Services.GetService<IChainService>());
        }

        public static ValidationCode ValidateTx(Transaction tx, IChainService chainService)
        {
            var inputSum = 0;

            // validate the hash first since it's simple
            if (tx.TransactionId != tx.GetTxIdAsString())
                return ValidationCode.Invalid;

            // validate the inputs
            foreach (var input in tx.Inputs)
            {
                // first, check if a tx already exists that references the same input
                var spend = chainService.GetTxByInput(input.PreviousTransactionId, input.PreviousOutputIndex).Result;
                if (spend != null)
                    return ValidationCode.Invalid;

                // get the referenced transaction data
                var prevTx = chainService.GetTx(input.PreviousTransactionId).Result;
                var prevOutput = prevTx.Outputs[input.PreviousOutputIndex];

                // compute the hashes and signatures
                var p2pkhValid = SHA256.Create().ComputeHash(input.UnlockerPublicKey).SequenceEqual(prevOutput.Address);
                var sigValid = Wallet.VerifyTransactionInputs(tx);

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
