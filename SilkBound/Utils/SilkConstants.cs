using System;
using System.Collections.Generic;
using System.Text;

namespace SilkBound.Utils
{
    public class SilkConstants
    {
        public const bool DEBUG = true;
        #region debug cheats
        public const bool INVULNERABILITY = false;
        public const bool GETALLPOWERUPS = true;
        #endregion

        public const bool CUSTOM_TITLE = true;
        
        public const short MAX_CLIENTS = 100;
        public const short MAX_NAME_LENGTH = 20;

        public const ushort PORT = 5000;
        public const short PACKET_BUFFER = 512;
        public const uint CONNECTION_TIMEOUT = 60;
        public const short CHUNK_TRANSFER = PACKET_BUFFER - byte.MaxValue - 16 - 1;
    }
}
