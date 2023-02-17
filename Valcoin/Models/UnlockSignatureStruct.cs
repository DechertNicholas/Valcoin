using System.Text.Json;

namespace Valcoin.Models
{
    public struct UnlockSignatureStruct
    {
        public ulong BlockNumber { get; set; }
        public byte[] PublicKey { get; set; }

        public static implicit operator byte[](UnlockSignatureStruct u) => JsonSerializer.SerializeToUtf8Bytes(u);

        public UnlockSignatureStruct(ulong blockNumber, byte[] publicKey)
        {
            this.BlockNumber = blockNumber;
            // use byte copy to avoid referencing the same object
            PublicKey = new byte[publicKey.Length];
            publicKey.CopyTo(PublicKey, 0);
        }
    }
}
