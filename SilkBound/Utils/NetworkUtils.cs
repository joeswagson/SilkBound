using SilkBound.Network;
using SilkBound.Network.Packets;
using SilkBound.Types;
using SilkBound.Types.NetLayers;
using System;
using System.Collections.Generic;
using System.Text;

namespace SilkBound.Utils
{
    public class NetworkUtils
    {
        public static Weaver? LocalClient;
        public static NetworkServer? LocalServer;
        public static PacketHandler? LocalPacketHandler;
        public static NetworkConnection? LocalConnection;
        public static bool IsServer
        {
            get
            {
                return LocalConnection is NetworkServer;
            }
        }
        public static bool IsConnected
        {
            get
            {
                return Server.CurrentServer != null || (LocalConnection?.IsConnected ?? false);
            }
        }

        public static Guid ClientID => LocalClient?.ClientID ?? Guid.Empty;
        public static bool SendPacket(Packet packet)
        {
            if (LocalConnection == null || !IsConnected) return false;
            LocalConnection.Send(packet);
            return true;
        }

        public static Weaver ConnectPipe(string host, string name)
        {
            return Connect(new NamedPipeConnection(host), name);
        }
        public static Weaver ConnectP2P(ulong steamId, string name)
        {
            return Connect(new SteamConnection(steamId.ToString()), name);
        }
        public static Weaver ConnectTCP(string host, string name, int? port=null)
        {
            return Connect(new TCPConnection(host, port), name);
        }
        public static Weaver Connect(NetworkConnection connection, string name)
        {
            if (LocalConnection != null)
                LocalConnection.Disconnect();

            LocalConnection = connection;
            LocalPacketHandler = connection.PacketHandler;
            LocalClient = LocalClient ?? new Weaver(name, connection);

            Server.CurrentServer = new Server(LocalConnection);

            Logger.Debug("Connection Completed:", connection.GetType().Name, name, LocalClient.ClientID);

            return LocalClient;
        }
    }
}
