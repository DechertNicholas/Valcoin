using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            _blockHeader = new BlockHeader();
            _blockHeader.Nonce = 0;
        }
    }
}
