using HutongGames.PlayMaker.Actions;
using SilkBound.Network;
using SilkBound.Network.Packets;
using SilkBound.Types;
using SilkBound.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SilkBound.Managers {
    public struct ConnectionResult {
        // target
        public string Address;
        public int? Port;

        // connection process
        public bool InProgress;
        public bool Succeeded;
        public bool HandshakeFulfilled;

        // returned types
        public Weaver Client;
        public Server Server;

        // network layer
        public NetworkingLayer NetworkLayer;
        public NetworkConnection Connection;
    }
    public class ConnectionManager {
        public static ConnectionResult Promise(NetworkingLayer networkingLayer, string ip, int? port = null) => new() {
            Address = ip,
            Port = port,

            InProgress = true,
            Succeeded = false,
            HandshakeFulfilled = false,

            NetworkLayer = networkingLayer
        };
        public static async Task<ConnectionResult> Client(NetworkingLayer networkingLayer, string ip, int? port = null, string? name = null)
        {
            name ??= ModMain.Config.Username;
            switch (networkingLayer)
            {
                case NetworkingLayer.TCP:
                    await NetworkUtils.ConnectTCPAsync(ip, name, port);
                    break;
                case NetworkingLayer.Steam:
                    await NetworkUtils.ConnectP2PAsync(ulong.Parse(ip), name);
                    break;
                case NetworkingLayer.NamedPipe:
                    await NetworkUtils.ConnectPipeAsync(ip, name);
                    break;
            }
        }

        public static void Server(NetworkingLayer networkingLayer)
        {

        }
    }
}
