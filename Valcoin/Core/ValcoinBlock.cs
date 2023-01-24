using Microsoft.UI.Xaml.Documents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Valcoin.Core
{
    public class ValcoinBlock
    {
        private byte[] _previousHash = new byte[32];
        private ulong _nonce = 0;
        private string _merkleRoot;
        private ulong _blockNumber;

        public byte[] PreviousHash
        {
            get { return _previousHash; }
            set { _previousHash = value; }
        }

        public ulong Nonce
        {
            get { return _nonce; }
            set { _nonce = value; }
        }

        public string MerkleRoot
        {
            get { return _merkleRoot; }
            set { _merkleRoot = value; }
        }

        public ulong BlockNumber
        {
            get { return _blockNumber; }
            set { _blockNumber = value; }
        }

        public static implicit operator byte[](ValcoinBlock b) => JsonSerializer.SerializeToUtf8Bytes(b);
    }
}
