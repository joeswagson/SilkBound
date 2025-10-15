using System;
using System.Collections.Generic;
using System.Text;

namespace SilkBound.Managers
{
    public class MagicByteManager
    {
        public static readonly byte[] SAVE_SIGNATURE = new byte[] { 0x53, 0x42, 0x53, 0x56 }; // SBSV
        public static readonly byte[] SKIN_SIGNATURE = new byte[] { 0x53, 0x42, 0x53, 0x4B }; // SBSK

        public static string Encode(byte[] signature)
        {
            return Encoding.UTF8.GetString(signature);
        }
    }
}
