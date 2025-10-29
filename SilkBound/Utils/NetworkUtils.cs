using SilkBound.Managers;
using SilkBound.Network;
using SilkBound.Network.Packets;
using SilkBound.Network.Packets.Handlers;
using SilkBound.Network.Packets.Impl.Communication;
using SilkBound.Types;
using SilkBound.Types.NetLayers;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace SilkBound.Utils
{
    public class NetworkUtils
    {
        public static LocalWeaver LocalClient = null!;
        public static Server Server => Server.CurrentServer;
        public static NetworkServer LocalServer = null!;
        public static NetworkConnection LocalConnection = null!;
        public static PacketHandler LocalPacketHandler = null!;
        public static ClientPacketHandler? ClientPacketHandler => LocalPacketHandler as ClientPacketHandler;
        public static ServerPacketHandler? ServerPacketHandler => LocalPacketHandler as ServerPacketHandler;
        public static AuthorityNode LocalAuthority => IsServer ? AuthorityNode.Server : AuthorityNode.Client; // ts so miniscule im not even putting it in the commit notes
        public static bool IsServer
        {
            get
            {
                return LocalConnection is NetworkServer;
            }
        }
        public static bool Connected
        {
            get
            {
                return (LocalConnection?.IsConnected ?? false) || Server.CurrentServer != null;
            }
        }

        public static event EventHandler OnConnected = delegate { };

        public static Guid ClientID => LocalClient?.ClientID ?? Guid.Empty;
        public static bool SendPacket(Packet packet)
        {
            Logger.Msg("send packet in nw utils");
            if (LocalConnection == null || !Connected) return false;
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
            Server.CurrentServer = new Server(LocalConnection);
        
            if (LocalConnection != null)
                LocalConnection.Disconnect();
            LocalConnection = connection;
            LocalPacketHandler = connection.PacketHandler;
            LocalClient ??= new LocalWeaver(name, connection);
            
            Logger.Debug("Connection Completed:", connection.GetType().Name, name, LocalClient.ClientID);
            return LocalClient;
        }
        public static void Disconnect(string reason="Unspecified", Weaver? target=null)
        {
            target ??= LocalClient;

            if (Connected && target == LocalClient)
                SendPacket(new ClientDisconnectionPacket(reason));

            HandleDisconnection(LocalConnection, reason);
        }
        internal static void HandleDisconnection(NetworkConnection connection, string reason="Unspecified")
        {
            Weaver? client = Server.CurrentServer?.GetWeaver(connection);
            if(client != null && NetworkUtils.IsServer)
                NetworkObjectManager.RevokeOwnership(client);

            if (client?.Mirror != null)
                UnityEngine.Object.Destroy(client.Mirror);

            connection.Disconnect();

            if(ModMain.Config.HostSettings.LogPlayerDisconnections)
                Logger.Msg($"{client?.ClientName ?? $"Connection {connection.Host}{(connection.Port.HasValue ? ":"+connection.Port.Value : string.Empty)}"}");
        }
        public static bool IsPacketThread()
        {
            StackTrace trace = new(true);
            StackFrame[] frames = trace.GetFrames() ?? [];

            foreach (var frame in frames)
            {
                MethodBase method = frame.GetMethod();
                if (method?.DeclaringType == null) continue;

                string fullName = $"{method.DeclaringType.FullName}.{method.Name}";
                if (fullName.Contains("PacketHandler"))
                {
                    //Logger.Msg("PacketHandler detected in stack trace:");
                    //foreach (var f in frames)
                    //{
                    //    MethodBase m = f.GetMethod();
                    //    string methodInfo = m?.DeclaringType != null
                    //        ? $"{m.DeclaringType.FullName}.{m.Name}"
                    //        : "<unknown>";
                    //    string fileInfo = f.GetFileName() != null ? $" at {f.GetFileName()}:{f.GetFileLineNumber()}" : "";
                    //    //Logger.Msg(methodInfo + fileInfo);
                    //}
                    return true;
                }
            }
            return false;
        }
        
        public static bool IsNullPtr([NotNullWhen(false)] UnityEngine.Object? obj) => obj == null || !Object.IsNativeObjectAlive(obj);
    }
}
