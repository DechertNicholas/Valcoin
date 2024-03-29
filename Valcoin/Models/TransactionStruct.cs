﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.Advertisement;

namespace Valcoin.Models
{
    /// <summary>
    /// A struct to hold the Transaction data when computing the hash and excluding other properties.
    /// </summary>
    public struct TransactionStruct
    {
        public int Version { get; set; }
        public List<TxInput> Inputs { get; set; }

        public List<TxOutput> Outputs { get; set; }

        public static implicit operator byte[](TransactionStruct t) => JsonSerializer.SerializeToUtf8Bytes(t);
    }
}
