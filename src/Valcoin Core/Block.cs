using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Valcoin_Core
{
    public class Block
    {
        [Key]
        public int BlockNumber;
        public int BlockVersion;
        public string PreviousHash;
        public DateTime BlockDateTime;
        public string TargetDifficulty;
        public string RootHash;
        public byte[] Nonce;
        //public HashTreeNode TxData;

        public static implicit operator byte[](Block b)
        {
            return JsonSerializer.SerializeToUtf8Bytes(b);
        }
    }
}
