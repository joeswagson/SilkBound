namespace SilkBound.Utils
{
    // netent = NetworkEntity

    public class SilkConstants
    {
        public const bool DEBUG =
            #if DEBUG
                true
            #else
                false
            #endif
        ;
        public const bool DEBUG_COLLIDERS = false;
        public const short TEST_CLIENTS = 1;
        #region debug cheats
        public const bool INVULNERABILITY = false;// DEBUG;
        public const bool GETALLPOWERUPS = DEBUG;
        #endregion

        public const bool CUSTOM_TITLE = true;

        public const short MAX_CLIENTS = 100; // TODO: never implement this (2 billion people need to play together)
        public const short MAX_NAME_LENGTH = 20;

        public const ushort PORT = 5000;
        public const short PACKET_BUFFER = 512;
        public const int CONNECTION_TIMEOUT = 5000; // ms
        public const short CHUNK_TRANSFER = PACKET_BUFFER - byte.MaxValue - 16 - 1;

        public class Server
        {
            public const bool REQUIRE_SERVER_NETENT_SYNC = false; // Server wont relay sync packets for NetworkEntities if the entity doent exist for the server.
        }
    }
}
