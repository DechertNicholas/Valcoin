using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Valcoin.Core
{
    public class BlockHeader
    {
        private string _previousHash;
        private ulong _nonce;
        private string _merkleRoot;

        public string PreviousHash
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
    }
}
