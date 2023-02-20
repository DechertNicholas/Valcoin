using Microsoft.EntityFrameworkCore;
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
    public class Wallet
    {
        /// <summary>
        /// Hex string representation of the hashed public key, for readability by people. Prefexed by '0x' to know that it is an address and not a block or transaction hash.
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
        /// The current balance of this wallet.
        /// </summary>
        public int Balance { get; set; }

        /// <summary>
        /// RSA implementation object that does not need to be re-created each time it is used.
        /// </summary>
        [NotMapped]
        private readonly ECDsa _ecdsa;

        /// <summary>
        /// Private constructor to get around EFCore's constructor weirdness. Even though a paramaterized constructor exists matching
        /// the values in the EFCore database, it will ALWAYS call the paramaterless constructor first. If <see cref="Create"/> were a
        /// constructor, a new <see cref="_ecdsa"/> would be created each call, yet the <see cref="PublicKey"/> and other properties
        /// would be set to the database values. This creates a mismatch between what is actually being used and what the user and
        /// developer thinks are being used, because properties are assigned AFTER instantiation - overwriting the generated property
        /// values. Moving the constructor to a <see cref="Create"/> method is not ideal, but I could not
        /// get EFCore to play nicely with parameterless and parameterized constructors.
        /// </summary>
        /// <param name="address">Hashed public key value, converted to hex string.</param>
        /// <param name="addressBytes">Hashed public key value.</param>
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
        /// <returns></returns>
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
        /// Signs data using the wallet's key pair.
        /// </summary>
        /// <param name="data">The data to sign. Normally used in a <see cref="TxOutput"/>'s <see cref="TxOutput.LockSignature"/>.</param>
        /// <returns></returns>
        public byte[] SignData(byte[] data)
        {
            return _ecdsa.SignData(data, HashAlgorithmName.SHA256);
        }

        /// <summary>
        /// Verifies data that was previously signed. 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="signature"></param>
        /// <returns></returns>
        public static bool VerifyData(byte[] data, byte[] signature, byte[] publicKey)
        {
            // TODO: Load the public key of the transaction since it won't always be our public key
            var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);

            ecdsa.ImportSubjectPublicKeyInfo(publicKey, out _);
            return ecdsa.VerifyData(data, signature, HashAlgorithmName.SHA256);
        }

        public void UpdateBalance()
        {
            var service = new StorageService();
            Balance = service.GetMyBalance();
        }
    }
}
