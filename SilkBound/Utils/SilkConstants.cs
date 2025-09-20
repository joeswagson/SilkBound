using System;
using System.Collections.Generic;
using System.Text;

namespace SilkBound.Utils
{
    public class SilkConstants
    {
        public const bool DEBUG = true;
        
        public const short MAX_CLIENTS = 100;
        public const short MAX_NAME_LENGTH = 20;

        public const ushort PORT = 30300;
        public const short PACKET_BUFFER = 512;
        public const short CHUNK_TRANSFER = PACKET_BUFFER - byte.MaxValue - 1;
    }
}
