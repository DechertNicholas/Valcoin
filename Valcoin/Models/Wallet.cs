﻿using Microsoft.EntityFrameworkCore;
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
    internal class Wallet
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
        private RSA _rsa;

        public void Initialize()
        {
            _rsa = RSA.Create();
            if (PublicKey != null)
            {
                _rsa.ImportRSAPublicKey(PublicKey, out _);
                _rsa.ImportRSAPrivateKey(PrivateKey, out _);
            }
            else
            {
                PublicKey = _rsa.ExportRSAPublicKey();
                PrivateKey = _rsa.ExportRSAPrivateKey(); // TODO: Decide if this will be encrypted
                AddressBytes = SHA256.Create().ComputeHash(PublicKey);
            }
        }

        public string GetAddressAsString()
        {
            return "0x" + Convert.ToHexString(AddressBytes);
        }

        public byte[] SignData(byte[] data)
        {
            return _rsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }
    }
}
