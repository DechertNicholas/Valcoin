using System;
using System.Collections.Generic;
using System.Text;

namespace Valcoin_Core
{
    public class HashTreeNode
    {
        // Will make left/right mutually exclusive from the Tx;
        public HashTreeNode Left;
        public HashTreeNode Right;
        public byte[] NodeSHA256Hash;
        public Transaction Tx;
    }


}
