using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Valcoin.Models
{
    internal class Wallet
    {
        public int Id { get; set; }

        public string Address
        {
            get => GetAddressAsString();
        }

        public byte[] AddressBytes { get; set; }

        public byte[] PublicKey { get; set; }
        
        public byte[] PrivateKey { get; set; }

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
    }
}
