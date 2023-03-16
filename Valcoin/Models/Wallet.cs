using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Valcoin.Services;

namespace Valcoin.Models
{
    /// <summary>
    /// A wallet is a public/private key pair that can unlock a locked TxOutput.
    /// </summary>
    public class Wallet
    {
        /// <summary>
        /// Hex string representation of the hashed public key, for readability by people. Prefexed by '0x' to know that it is an address and not a block or transaction hash.
        /// This prefix hopefully helps prevent the accidental sending of Valcoin to other hashes, like a block hash or transaction hash.
        /// </summary>
        [Key]
        public string Address { get; set; }

        /// <summary>
        /// The raw bytes of the hashed public key.
        /// </summary>
        public byte[] AddressBytes { get; set; }

        /// <summary>
        /// The public key for the wallet.
        /// </summary>
        public byte[] PublicKey { get; set; }
        
        /// <summary>
        /// The private key for the wallet.
        /// </summary>
        public byte[] PrivateKey { get; set; }

        /// <summary>
        /// RSA implementation object that does not need to be re-created each time it is used.
        /// </summary>
        [NotMapped]
        private readonly ECDsa _ecdsa;

        /// <summary>
        /// Constructor to get around EFCore's constructor weirdness. Even though a paramaterized constructor exists matching
        /// the values in the EFCore database, it will ALWAYS call the paramaterless constructor first. If <see cref="Create"/> were a
        /// constructor, a new <see cref="_ecdsa"/> would be created each call, yet the <see cref="PublicKey"/> and other properties
        /// would be set to the database values. This creates a mismatch between what is actually being used and what the user and
        /// developer thinks are being used, because properties are assigned AFTER instantiation - overwriting the generated property
        /// values. Moving the constructor to a <see cref="Create"/> method is not ideal, but I could not
        /// get EFCore to play nicely with parameterless and parameterized constructors.
        /// </summary>
        /// <param name="publicKey">The public key.</param>
        /// <param name="privateKey">The private key.</param>
        public Wallet(byte[] publicKey, byte[] privateKey)
        {
            _ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);

            _ecdsa.ImportSubjectPublicKeyInfo(publicKey, out _);
            _ecdsa.ImportECPrivateKey(privateKey, out _);

            PublicKey = _ecdsa.ExportSubjectPublicKeyInfo();
            PrivateKey = _ecdsa.ExportECPrivateKey();

            AddressBytes = SHA256.Create().ComputeHash(publicKey);
            Address = GetAddressAsString();
        }

        /// <summary>
        /// Creates a new instance of Wallet. To be used in place of a constructor.
        /// </summary>
        /// <returns>A wallet with a new public/private key pair.</returns>
        public static Wallet Create()
        {
            var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            return new Wallet(ecdsa.ExportSubjectPublicKeyInfo(), ecdsa.ExportECPrivateKey());
        }

        /// <summary>
        /// Returns the AddressBytes as a hex string, prefixed with '0x' to signify that it is an address.
        /// </summary>
        /// <returns></returns>
        public string GetAddressAsString()
        {
            return "0x" + Convert.ToHexString(AddressBytes);
        }

        /// <summary>
        /// Sets the <see cref="TxInput.UnlockSignature"/> property in a transaction. The transaction data is converted to a byte[] by using
        /// an <see cref="UnlockSignatureStruct"/> before siging that data, and setting the result as the UnlockSignature.
        /// </summary>
        /// <param name="tx"></param>
        public void SignTransactionInputs(ref Transaction tx)
        {
            var sig = _ecdsa.SignData(new UnlockSignatureStruct(tx), HashAlgorithmName.SHA256);
            // assign the signature to all inputs
            tx.Inputs.ForEach(i => i.UnlockSignature = sig);

            // we changed the data, need to set the txid as well
            tx.ComputeAndSetTransactionId();
        }

        /// <summary>
        /// Verifies the <see cref="TxInput.UnlockSignature"/> that was previously signed.
        /// </summary>
        /// <param name="tx">The transaction containing the inputs.</param>
        /// <returns>A boolean indicating the success or failure of the verification.</returns>
        public static bool VerifyTransactionInputs(Transaction tx)
        {
            var pubKey = tx.Inputs.First().UnlockerPublicKey;
            var sig = tx.Inputs.First().UnlockSignature;

            var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            // all public keys will be the same, so we can use the first one
            ecdsa.ImportSubjectPublicKeyInfo(pubKey, out _);

            return ecdsa.VerifyData(new UnlockSignatureStruct(tx), sig, HashAlgorithmName.SHA256);
        }
    }
}
