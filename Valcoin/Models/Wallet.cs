using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Valcoin.Models
{
    internal class Wallet
    {
        public byte[] Address { get; set; } = new byte[32];

        public byte[] PublicKey { get; set; }

        private readonly RSA _rsa;

        public Wallet()
        {
            _rsa = RSA.Create(); // load this from the DB instead
            PublicKey = _rsa.ExportRSAPublicKey();
            Address = SHA256.Create().ComputeHash(PublicKey);
        }

        public string GetAddressAsString()
        {
            return Encoding.UTF8.GetString(Address);
        }
    }
}
