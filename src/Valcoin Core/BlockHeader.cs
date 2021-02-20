using System;
using System.Collections.Generic;
using System.Text;

namespace Valcoin_Core
{
    public class BlockHeader
    {
        public int BlockVersion;
        public string PreviousHash;
        public DateTime BlockDateTime;
        public string TargetDifficulty;
        public string RootHash;
        public byte[] Nonce;
    }
}
