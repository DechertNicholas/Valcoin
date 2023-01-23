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
        private BlockHeader _blockHeader;

        public BlockHeader BlockHeader
        {
            get { return _blockHeader; }
            set { _blockHeader = value; }
        }

        public ValcoinBlock()
        {
            _blockHeader = new BlockHeader
            {
                Nonce = 0
            };
        }

        public static implicit operator byte[](ValcoinBlock b) => JsonSerializer.SerializeToUtf8Bytes(b);
    }
}
