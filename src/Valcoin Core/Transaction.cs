using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Valcoin_Core
{
    public class Transaction
    {
        //public byte[] currentOwnerPublicKey;
        //public byte[] previousOwnerSignature;
        //public byte[] transactionHash;
        public decimal Amount; // limit the amount to 8 decimal places (11.12345678, for example)

        public static implicit operator byte[](Transaction tx)
        {
            return JsonSerializer.SerializeToUtf8Bytes(tx);
        }
    }

    
}
