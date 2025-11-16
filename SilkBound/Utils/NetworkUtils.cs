using SilkBound.Behaviours;
using SilkBound.Extensions;
using SilkBound.Managers;
using SilkBound.Network;
using SilkBound.Network.NetworkLayers;
using SilkBound.Network.NetworkLayers.Impl;
using SilkBound.Network.Packets;
using SilkBound.Network.Packets.Handlers;
using SilkBound.Network.Packets.Impl.Communication;
using SilkBound.Types;
using SilkBound.Types.Language;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using static SilkBound.Managers.Error;

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
                return (LocalConnection?.IsConnected ?? false) || (Server.IsOnline && !Server.Disposed);
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
                    ConnectionManager.ConnectionFailed(request, Error.CLIENT.AMBIDEXTROUS);

                return;
            }

            SendPacket(new HandshakePacket(client.ClientID, client.ClientName));
        }

        #region Async Connectors
        public static async Task<Weaver> ConnectPipeAsync(CancellationTokenSource cts, ConnectionRequest request, string host, string name)
        {
            return await ConnectAsync(cts, new NamedPipeConnection(host), name, request);
        }
        public static async Task<Weaver> ConnectP2PAsync(CancellationTokenSource cts, ConnectionRequest request, ulong steamId, string name)
        {
            return await ConnectAsync(cts, new SteamConnection(steamId.ToString()), name, request);
        }
        public static async Task<Weaver> ConnectTCPAsync(CancellationTokenSource cts, ConnectionRequest request, string host, string name, int? port = null)
        {
            return await ConnectAsync(cts, new TCPConnection(host, port ?? Silkbound.Config.Port), name, request);
        }
        public static async Task<Weaver> ConnectAsync(CancellationTokenSource cts, NetworkConnection connection, string name, ConnectionRequest? request = null)
        {
            Server.CurrentServer?.Dispose();
            Server.CurrentServer = new(connection);

            LocalConnection?.Dispose();
            LocalConnection?.Disconnect();
            LocalConnection = connection;
            LocalPacketHandler = connection.PacketHandler;
            LocalClient = new LocalWeaver(
                name,
                connection,
                SilkConstants.CLIENT_RECONNECT_PERSIST_ID
                    ? LocalClient?.ClientID
                    : null);

            Server.CurrentServer.Connections.Add(LocalClient);

            Server.CurrentServer.Address = connection.Host!;
            Server.CurrentServer.Port = connection.Port ?? Silkbound.Config.Port;


            var connectTask = connection.Connect(
                Server.CurrentServer.Address,
                Server.CurrentServer.Port
            );

            var cancelledTask = Task.Delay(SilkConstants.CONNECTION_TIMEOUT + 5000, cts.Token);

            var winner = await Task.WhenAny(connectTask, cancelledTask);

            if (winner == cancelledTask)
                cts.Token.ThrowIfCancellationRequested();

            if (LocalConnection is not NetworkServer)
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
            Server.CurrentServer?.Dispose();
            Server.CurrentServer = new(LocalConnection) {
                Connection = connection
            };

            LocalConnection?.Disconnect();

            LocalConnection = connection;
            LocalPacketHandler = connection.PacketHandler;
            LocalClient = new LocalWeaver(
                name,
                connection,
                SilkConstants.CLIENT_RECONNECT_PERSIST_ID
                    ? LocalClient?.ClientID
                    : null);

            Server.CurrentServer.Connections.Add(LocalClient);

            Server.CurrentServer.Address = connection.Host!;
            Server.CurrentServer.Port = connection.Port ?? Silkbound.Config.Port;

            connection.Connect(
                Server.CurrentServer.Address,
                Server.CurrentServer.Port
            ).Await();

            Handshake(null, LocalClient);

            Logger.Debug("Connection Completed:", connection.GetType().Name, name, LocalClient.ClientID);
            return LocalClient;
        }
        #endregion
        /// <summary>
        /// Handles local and remote disconnections.
        /// </summary>
        /// <param name="reason">Reason for disconnection.</param>
        /// <param name="target">If present, the client to disconnect; <see cref="LocalClient"/> otherwise.</param>
        public static void Disconnect(string reason = "Unspecified", Weaver? target = null)
        {
            if (LocalConnection == null) // sorry nix
                return;

            target ??= LocalClient;

            if (Connected && target == LocalClient && !IsServer)
                SendPacket(new ClientDisconnectionPacket(reason));

            if (target.Connection != null)
                HandleDisconnection(target.Connection, reason);
            else
                Logger.Error("Couldn't disconnect", target.ClientName, target.ClientID, "because they do not have an associated network connection.");
        }
        /// <summary>
        /// Disconnects a networking layer NetworkConnection.
        /// </summary>
        public static void Disconnect(NetworkConnection connection, string reason = "Unspecified")
        {
            if (connection == LocalConnection && !IsServer)
                SendPacket(new ClientDisconnectionPacket(reason));

            HandleDisconnection(connection, reason);
        }

        internal static void HandleLocalDisconnection(Weaver? client, NetworkConnection connection, string reason)
        {
            // clean up all the mirrors
            foreach(var mirror in HornetMirror.Mirrors)
                Object.Destroy(mirror.Value.gameObject);

            // reset server so we can reconnect under a new one without the old interfering with uis
            Server.CurrentServer = null!;
        }

        /// <summary>
        /// Internal finalizer for resources and connection closing.
        /// </summary>
        internal static void HandleDisconnection(NetworkConnection connection, string reason = "Unspecified")
        {
            // attempt to get a client rep
            Weaver? client = Server?.GetWeaver(connection);

            // take ownership from the client
            if (client != null && IsServer)
                NetworkObjectManager.RevokeOwnership(client);

            // mirror destruction
            if (client?.Mirror != null)
                Object.Destroy(client.Mirror);

            // reroute to local handler
            if (connection == LocalConnection)
                HandleLocalDisconnection(client, connection, reason);

            // remove from connection list
            if (Server != null && client != null)
                Server.Connections.Remove(client);

            // shutdown the server if we are trying to
            if (connection is NetworkServer server && server == LocalConnection)
                Server?.Shutdown();

            // close network connection
            connection.Dispose();

            if (Silkbound.Config.HostSettings.LogPlayerDisconnections)
                Logger.Msg($"{client?.ClientName ?? $"{connection.Host}{(connection.Port.HasValue ? ":" + connection.Port.Value : string.Empty)}"} disconnected: {reason}");
        }

        /// <summary>
        /// Checks the current callstack for signs of packet handling.
        /// </summary>
        /// <returns>Whether or not the current execution context was called from a packet handler.</returns>
        public static bool IsPacketThread()
        {
            StackTrace trace = new(true);
            StackFrame[] frames = trace.GetFrames() ?? [];

            foreach (var frame in frames)
            {
                MethodBase method = frame.GetMethod();
                if (method?.DeclaringType == null) continue;

                string fullName = $"{method.DeclaringType.FullName}.{method.Name}";
                if (fullName.Contains("PacketHandler") || typeof(Packet).IsAssignableFrom(method.DeclaringType))
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
        /// <summary>
        /// Dispatches a packet to be sent and immediately resumes without awaiting.
        /// </summary>
        public static void SendPacket(Packet packet)
        {
            //Logger.Msg("send packet in nw utils");
            if (LocalConnection == null || !Connected) return;
            //Logger.Msg(MethodInfo.GetCurrentMethod().Name, "waiting");
            //Task t = Task.Run(async () => {
            //    await LocalConnection.Send(packet);
            //});
            //t.Wait();
            //Logger.Msg(MethodInfo.GetCurrentMethod().Name, "waited");
            LocalConnection.Send(packet).Void();
        }

        /// <summary>
        /// Asynchronously sends and awaits a call to send the packet.
        /// </summary>
        /// <returns>Result is <see langword="true"/> if the packet sent successfully.</returns>
        public static async Task<bool> SendPacketAsync(Packet packet)
        {
            //Logger.Msg("send packet in nw utils");
            if (LocalConnection == null || !Connected) return false;
            Task t = LocalConnection.Send(packet);
            await t;
            return t.IsCompletedSuccessfully;
        }
        /// <summary>
        /// Null checks a UnityObject using its native engine pointer and a managed null comparison.
        /// </summary>
        /// <returns><see langword="true"/> if the objects <see cref="Object.m_CachedPtr"/> is null, or the object reference itself was null.</returns>
        public static bool IsNullPtr([NotNullWhen(false)] UnityEngine.Object? obj) => obj == null || !Object.IsNativeObjectAlive(obj);
        public static Weaver? GetWeaver(Guid target) => Server.GetWeaver(target);
        public static Weaver? GetWeaver(NetworkConnection connection) => Server.GetWeaver(connection);
    }
}
