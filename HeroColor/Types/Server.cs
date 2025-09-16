using SilkBound.Network;
using SilkBound.Utils;
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

        public Dictionary<Weaver, NetworkConnection> Connections = new Dictionary<Weaver, NetworkConnection>();

        public void Kick(Weaver weaver)
        {
            if (NetworkUtils.IsServer)
            {
                weaver.Disconnect();

                Connections.Remove(weaver);
            }
        }

        public Weaver? Host { get; internal set; }
        public string? Address { get; internal set; }
        public int? Port { get; internal set; } = 30300; // default port
        public NetworkConnection? Connection { get; internal set; }
        public static Server ConnectPiped(string host, string name)
        {
            NetworkServer connection = new NamedPipeServer(host);
            NetworkUtils.Connect(connection, name);
            return new Server(connection);
        }
    }
}
