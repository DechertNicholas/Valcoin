using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.Advertisement;

namespace Valcoin.Models
{
    public struct TransactionStruct
    {
        public int Version { get; set; }
        public TxInput[] Inputs { get; set; }

        public TxOutput[] Outputs { get; set; }

        public static implicit operator byte[](TransactionStruct t) => JsonSerializer.SerializeToUtf8Bytes(t);
    }
}
