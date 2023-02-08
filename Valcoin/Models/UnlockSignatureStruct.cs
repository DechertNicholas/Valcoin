using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Valcoin.Models
{
    public struct UnlockSignatureStruct
    {
        public ulong BlockId { get; set; }
        public byte[] PublicKey { get; set; }

        public static implicit operator byte[](UnlockSignatureStruct u) => JsonSerializer.SerializeToUtf8Bytes(u);
    }
}
