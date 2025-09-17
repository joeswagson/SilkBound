using SilkBound.Network;
using SilkBound.Network.Packets;
using SilkBound.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace SilkBound.Utils
{
    public class NetworkUtils
    {
        public static Weaver? LocalClient;
        public static PacketHandler? LocalPacketHandler;
        public static NetworkConnection? LocalConnection;
        public static bool IsServer
        {
            get
            {
                return Server.CurrentServer?.Host == LocalClient;
            }
        }
        public static bool IsConnected
        {
            get
            {
                return Server.CurrentServer != null;
            }
        }

        public static void Connect(NetworkConnection connection, string name)
        {
            if (LocalConnection != null)
                LocalConnection.Disconnect();

            LocalConnection = connection;
            LocalPacketHandler = connection.PacketHandler;
            LocalClient = LocalClient ?? new Weaver(connection) { ClientName=name };
        }
    }
}
