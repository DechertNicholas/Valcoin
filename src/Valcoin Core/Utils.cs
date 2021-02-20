using System;
using System.Collections.Generic;
using System.Text;

namespace Valcoin_Core
{
    public class Utils
    {
        public static string HashByteToString(byte[] byteHash)
        {
            var hexSb = new StringBuilder(byteHash.Length * 2);
            foreach (byte b in byteHash)
            {
                hexSb.AppendFormat("{0:x2}", b);
            }
            //debug
            Console.WriteLine($"Created hex: {hexSb}");
            return hexSb.ToString();
        }

        public static byte[] StringToByteArray(string hexString)
        {
            int numberOfChars = hexString.Length;
            byte[] bytes = new byte[numberOfChars / 2];
            for (int i = 0; i < numberOfChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hexString.Substring(i, 2), 16);
            return bytes;
        }
    }
}
