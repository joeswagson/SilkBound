namespace SilkBound.Utils
{
    // netent = NetworkEntity

    public class SilkConstants
    {
        public const bool DEBUG = true;
        public const bool DEBUG_COLLIDERS = false;
        public const short TEST_CLIENTS = 2;
        #region debug cheats
        public const bool INVULNERABILITY =
            #if DEBUG
                false
            #else
                false
            #endif
        ;
        public const bool GETALLPOWERUPS = true;
        #endregion

        public const bool CUSTOM_TITLE = true;

        public const short MAX_CLIENTS = 100; // TODO: never implement this (2 billion people need to play together)
        public const short MAX_NAME_LENGTH = 20;

        public const ushort PORT = 5000;
        public const short PACKET_BUFFER = 512;
        public const uint CONNECTION_TIMEOUT = 60;
        public const short CHUNK_TRANSFER = PACKET_BUFFER - byte.MaxValue - 16 - 1;

        public class Server
        {
            public const bool REQUIRE_SERVER_NETENT_SYNC = true; // Server wont relay sync packets for NetworkEntities if the entity doent exist for the server.
        }
    }
}
