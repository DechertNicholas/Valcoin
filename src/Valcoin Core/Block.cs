using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Valcoin_Core
{
    public class Block
    {
        public BlockHeader Header;
        public HashTreeNode TxData;

        public static implicit operator byte[](Block b)
        {
            return JsonSerializer.SerializeToUtf8Bytes(b);
        }
    }
}
