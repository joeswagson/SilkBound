using SilkBound.Managers;
using System;

namespace SilkBound.Utils
{
    // netent = NetworkEntity

    public class SilkConstants
    {
        public static readonly bool DEBUG = 
            #if DEBUG
                true
            #else
                LocalProps.Safe<bool>("DEBUG")
            #endif
        ;
        public static readonly bool DEBUG_COLLIDERS = LocalProps.Safe<bool>("DRAW_COLLIDERS");
        public const short TEST_CLIENTS = 2;
        #region debug cheats
        public static readonly bool INVULNERABILITY = LocalProps.Safe<bool>("IMMORTAL");// DEBUG
        public static readonly bool GETALLPOWERUPS = LocalProps.Safe<bool>("POWERUPS");
        #endregion

        public const bool CUSTOM_TITLE = true;

        public const short MAX_CLIENTS = 100; // TODO: never implement this (2 billion people need to play together)
        public const short MAX_NAME_LENGTH = 20;

        public const ushort PORT = 30300;
        public const short PACKET_BUFFER = 512;
        public const int CONNECTION_TIMEOUT = 5000; // ms
        public const short CHUNK_TRANSFER = PACKET_BUFFER - sizeof(ushort) - 16 - 1;

        public const bool CLIENT_RECONNECT_PERSIST_ID = true;

        public class Server
        {
            public const bool REQUIRE_SERVER_NETENT_SYNC = false; // Server wont relay sync packets for NetworkEntities if the entity doent exist for the server.
        }
    }
}
