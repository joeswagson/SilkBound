using SilkBound.Behaviours;
using SilkBound.Managers;
using SilkBound.Network;
using SilkBound.Network.Packets;
using SilkBound.Network.Packets.Handlers;
using SilkBound.Network.Packets.Impl.Communication;
using SilkBound.Types;
using SilkBound.Types.Language;
using SilkBound.Types.NetLayers;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading.Tasks;

namespace SilkBound.Utils {
    public class NetworkUtils {
        public static LocalWeaver LocalClient = null!;
        private static HornetMirror? _lMirror;
        public static HornetMirror? LocalMirror => _lMirror ??= LocalClient?.Mirror;
        public static Server Server => Server.CurrentServer;
        public static ServerSettings ServerSettings => Server.Settings;
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

        public static void Handshake(ConnectionRequest? request, LocalWeaver? client)
        {
            if (client == null)
            {
                if (request != null)
                    ConnectionManager.ConnectionFailed(request);

                return;
            }

            if ((request?.HandshakeFulfilled ?? false) || client.Acknowledged)
            {
                if (request != null)
                    ConnectionManager.ConnectionFailed(request, Error.Client.AMBIDEXTROUS);

                return;
            }

            SendPacket(new HandshakePacket(client.ClientID, client.ClientName));
        }

        #region Async Connectors
        public static async Task<Weaver> ConnectPipeAsync(ConnectionRequest request, string host, string name)
        {
            return await ConnectAsync(new NamedPipeConnection(host), name, request);
        }
        public static async Task<Weaver> ConnectP2PAsync(ConnectionRequest request, ulong steamId, string name)
        {
            return await ConnectAsync(new SteamConnection(steamId.ToString()), name, request);
        }
        public static async Task<Weaver> ConnectTCPAsync(ConnectionRequest request, string host, string name, int? port = null)
        {
            return await ConnectAsync(new TCPConnection(host, port ?? Silkbound.Config.Port), name, request);
        }
        public static async Task<Weaver> ConnectAsync(NetworkConnection connection, string name, ConnectionRequest? request = null)
        {
            Server.CurrentServer = new Server(LocalConnection);
            Server.CurrentServer.Connection = connection;

            if (LocalConnection != null)
                LocalConnection.Disconnect();

            LocalConnection = connection;
            LocalPacketHandler = connection.PacketHandler;
            LocalClient ??= new LocalWeaver(name, connection);
            Server.CurrentServer.Connections.Add(LocalClient);

            Server.CurrentServer.Address = connection.Host!;
            Server.CurrentServer.Port = connection.Port ?? Silkbound.Config.Port;

            await connection.Connect(
                Server.CurrentServer.Address,
                Server.CurrentServer.Port
            );

            if(LocalConnection is not NetworkServer)
                Handshake(request, LocalClient);

            Logger.Debug("Connection Completed:", connection.GetType().Name, name, LocalClient.ClientID);
            return LocalClient;
        }
        #endregion
        #region Synchronized Connectors
        public static Weaver ConnectPipe(string host, string name)
        {
            return Connect(new NamedPipeConnection(host), name);
        }
        public static Weaver ConnectP2P(ulong steamId, string name)
        {
            return Connect(new SteamConnection(steamId.ToString()), name);
        }
        public static Weaver ConnectTCP(string host, string name, int? port = null)
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
            Server.CurrentServer.Connections.Add(LocalClient);

            Server.CurrentServer.Address = connection.Host!;
            Server.CurrentServer.Port = connection.Port ?? Silkbound.Config.Port;

            connection.Connect(
                Server.CurrentServer.Address,
                Server.CurrentServer.Port
            ).Wait();

            Handshake(null, LocalClient);

            Logger.Debug("Connection Completed:", connection.GetType().Name, name, LocalClient.ClientID);
            return LocalClient;
        }
        #endregion
        public static void Disconnect(string reason = "Unspecified", Weaver? target = null)
        {
            if (LocalConnection == null) // sorry nix
                return;

            target ??= LocalClient;

            if (Connected && target == LocalClient)
                SendPacket(new ClientDisconnectionPacket(reason));

            HandleDisconnection(LocalConnection, reason);
        }
        public static void Disconnect(NetworkConnection connection, string reason = "Unspecified")
        {
            if (connection == LocalConnection)
                SendPacket(new ClientDisconnectionPacket(reason));

            HandleDisconnection(connection, reason);
        }
        internal static void HandleDisconnection(NetworkConnection connection, string reason = "Unspecified")
        {
            Weaver? client = Server.CurrentServer?.GetWeaver(connection);
            if (client != null && IsServer)
                NetworkObjectManager.RevokeOwnership(client);

            if (client?.Mirror != null)
                Object.Destroy(client.Mirror);

            if (connection is NetworkServer server && server == LocalConnection)
                Server.CurrentServer!.Shutdown();

            connection.Dispose();

            if (Silkbound.Config.HostSettings.LogPlayerDisconnections)
                Logger.Msg($"{client?.ClientName ?? $"{connection.Host}{(connection.Port.HasValue ? ":" + connection.Port.Value : string.Empty)}"} disconnected: {reason}");
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
        public static bool SendPacket(Packet packet)
        {
            //Logger.Msg("send packet in nw utils");
            if (LocalConnection == null || !Connected) return false;
            LocalConnection.Send(packet);
            return true;
        }
        public static bool IsNullPtr([NotNullWhen(false)] UnityEngine.Object? obj) => obj == null || !Object.IsNativeObjectAlive(obj);
        public static Weaver? GetWeaver(Guid target) => Server.GetWeaver(target);
        public static Weaver? GetWeaver(NetworkConnection connection) => Server.GetWeaver(connection);
    }
}
