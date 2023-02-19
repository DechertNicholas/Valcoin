using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Valcoin.Models;

namespace Valcoin.Helpers
{
    public class TxOutputComparer : IEqualityComparer<TxOutput>
    {
        public bool Equals(TxOutput x, TxOutput y)
        {
            //Check whether the compared objects reference the same data.
            if (ReferenceEquals(x, y)) return true;

            //Check whether any of the compared objects is null.
            if (x is null || y is null)
                return false;

            //Check whether the products' properties are equal.
            return x.Amount == y.Amount && x.LockSignature.SequenceEqual(y.LockSignature);
        }

        public int GetHashCode([DisallowNull] TxOutput output)
        {
            //Check whether the object is null
            if (output is null) return 0;

            //Get hash code for the Name field if it is not null.
            int hashOutputAmount = output.Amount.GetHashCode();

            //Get hash code for the Code field.
            int hashOutputSig = output.LockSignature.GetHashCode();

            //Calculate the hash code for the product.
            return hashOutputAmount ^ hashOutputSig;
        }
    }
}
