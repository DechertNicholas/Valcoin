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
        /// Specifically for Entity Framework Core tracking. Not used in the Valcoin protocol.
        /// </summary>
        public int WalletId { get; private set; }

        /// <summary>
        /// Hex string representation of the hashed public key, for readability by people. Prefexed by '0x' to know that it is an address and not a block or transaction hash.
        /// </summary>
        public string Address
        {
            get => GetAddressAsString();
        }

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

        public Wallet()
        {
            _rsa = RSA.Create();
            
            PublicKey = _rsa.ExportRSAPublicKey();
            PrivateKey = _rsa.ExportRSAPrivateKey(); // TODO: Decide if this will be encrypted
            AddressBytes = SHA256.Create().ComputeHash(PublicKey);
        }

        public Wallet(byte[] publicKey, byte[] privateKey)
        {
            _rsa = RSA.Create();
            PublicKey = publicKey;
            PrivateKey = privateKey;
            _rsa.ImportRSAPublicKey(publicKey, out _);
            _rsa.ImportRSAPrivateKey(privateKey, out _);
            AddressBytes = SHA256.Create().ComputeHash(publicKey);
        }

        public string GetAddressAsString()
        {
            return "0x" + Convert.ToHexString(AddressBytes);
        }

        public byte[] SignData(byte[] data)
        {
            return _rsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }

        public bool VerifyData(byte[] data, byte[] signature)
        {
            return _rsa.VerifyData(data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }
    }
}
