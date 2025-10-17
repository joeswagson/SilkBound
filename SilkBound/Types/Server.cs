using SilkBound.Network;
using SilkBound.Network.Packets;
using SilkBound.Network.Packets.Impl;
using SilkBound.Types.NetLayers;
using SilkBound.Utils;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Text;
using SilkBound.Addons.AddonLoading;
using SilkBound.Network.Packets.Impl.Steam;

namespace SilkBound.Types
{
    public class Server
    {
        public Server(NetworkServer connection)
        {
            Connection = connection;
        }
        public Server(NetworkConnection connection)
        {
            Connection = connection;
        }

        public static Server CurrentServer = null!;
        public static bool IsOnline
        {
            get
            {
                return CurrentServer != null;
            }
        }

        public List<Weaver> Connections = new List<Weaver>();

        public Weaver? GetWeaver(Guid clientId)
        {
            return Connections.Find(c => c.ClientID == clientId);
        }
        public Weaver? GetWeaver(NetworkConnection connection)
        {
            return Connections.Find(c => c.Connection == connection);
        }
        public void Kick(Weaver weaver)
        {
            if (!NetworkUtils.IsServer)
                return;

            if (Connection is SteamServer server)
            {
                CSteamID weaverId = ((SteamConnection)weaver.Connection).RemoteId;

                foreach (Weaver connection in Connections)
                {
                    if (weaver != NetworkUtils.LocalClient)
                    {
                        connection.Connection.Send(new SteamKickS2CPacket(weaverId.m_SteamID));
                        break;
                    }
                }

                Connections.Remove(weaver);
            }
            else
            {
                
            }
        }

        public Weaver Host { get; internal set; } = null!;
        public string Address { get; internal set; } = null!;
        public int? Port { get; internal set; }
        public NetworkConnection Connection { get; internal set; } = null!;
        public static Server ConnectPipe(string host, string name)
        {
            return Connect(new NamedPipeServer(host), name);
        }
        public static Server ConnectP2P(string name)
        {
            return Connect(new SteamServer(), name);
        }
        public static Server ConnectTCP(string host, string name, int? port = null)
        {
            return Connect(new TCPServer(host, port), name);
        }

        public static Server Connect(NetworkServer connection, string name)
        {
            NetworkUtils.LocalServer = connection;
            NetworkUtils.Connect(connection, name);

            CurrentServer = new Server(connection);
            CurrentServer.Address = connection.Host;
            CurrentServer.Port = connection.Port ?? CurrentServer.Port ?? SilkConstants.PORT;

            AddonManager.LoadAddons();

            return CurrentServer;
        }
    }
}
