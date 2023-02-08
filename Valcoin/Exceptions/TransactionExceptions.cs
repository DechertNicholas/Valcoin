using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Valcoin.Exceptions.TransactionExceptions
{
    public class InvalidSendingAmountException : Exception
    {
        public InvalidSendingAmountException()
        {
        }

        public InvalidSendingAmountException(string message)
            : base(message)
        {
        }

        public InvalidSendingAmountException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }

    public class InvalidRecipientAddressException : Exception
    {
        public InvalidRecipientAddressException()
        {
        }

        public InvalidRecipientAddressException(string message)
            : base(message)
        {
        }

        public InvalidRecipientAddressException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
