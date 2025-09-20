using SilkBound.Network;
using SilkBound.Network.Packets;
using SilkBound.Network.Packets.Impl;
using SilkBound.Types.NetLayers;
using SilkBound.Utils;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Text;

namespace SilkBound.Types
{
    public class Server
    {
        public Server(NetworkServer connection)
        {
            Connection = connection;
        }

        public static Server? CurrentServer;
        public static bool IsOnline
        {
            get
            {
                return CurrentServer != null;
            }
        }

        public List<Weaver> Connections = new List<Weaver>();

        public void Kick(Weaver weaver)
        {
            if (NetworkUtils.IsServer && Connection is SteamServer)
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
        }

        public Weaver? Host { get; internal set; }
        public string? Address { get; internal set; }
        public int? Port { get; internal set; }
        public NetworkConnection? Connection { get; internal set; }
        public static Server ConnectPipe(string host, string name)
        {
            return Connect(new NamedPipeServer(host), name);
        }
        public static Server ConnectP2P(string name)
        {
            return Connect(new SteamServer(), name);
        }
        public static Server ConnectTCP(string host, string name, int? port=null)
        {
            return Connect(new TCPServer(host, port), name);
        }

        public static Server Connect(NetworkServer connection, string name)
        {
            NetworkUtils.Connect(connection, name);
            CurrentServer = new Server(connection);
            CurrentServer.Port = connection.Port ?? CurrentServer.Port ?? SilkConstants.PORT;
            return CurrentServer;
        }
    }
}
