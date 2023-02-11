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
        /// RSA implementation object that does not need to be re-created each time it is used.
        /// </summary>
        [NotMapped]
        private readonly RSA _rsa;

        /// <summary>
        /// Creates a new wallet with a new key pair.
        /// </summary>
        public Wallet()
        {
            _rsa = RSA.Create();
            
            PublicKey = _rsa.ExportRSAPublicKey();
            PrivateKey = _rsa.ExportRSAPrivateKey(); // TODO: Decide if this will be encrypted
            AddressBytes = SHA256.Create().ComputeHash(PublicKey);
            Address = Convert.ToHexString(AddressBytes);
        }

        /// <summary>
        /// Creates a wallet with existing key pair. Used for loading from the database.
        /// </summary>
        /// <param name="publicKey"></param>
        /// <param name="privateKey"></param>
        public Wallet(byte[] publicKey, byte[] privateKey)
        {
            _rsa = RSA.Create();
            PublicKey = publicKey;
            PrivateKey = privateKey;
            _rsa.ImportRSAPublicKey(publicKey, out _);
            _rsa.ImportRSAPrivateKey(privateKey, out _);
            AddressBytes = SHA256.Create().ComputeHash(publicKey);
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
            return _rsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }

        /// <summary>
        /// Verifies data that was previously signed. 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="signature"></param>
        /// <returns></returns>
        public bool VerifyData(byte[] data, byte[] signature)
        {
            // TODO: Load the public key of the transaction since it won't always be our public key
            return _rsa.VerifyData(data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }
    }
}
