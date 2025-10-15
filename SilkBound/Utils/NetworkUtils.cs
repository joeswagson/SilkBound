using SilkBound.Network;
using SilkBound.Network.Packets;
using SilkBound.Network.Packets.Handlers;
using SilkBound.Network.Packets.Impl.Communication;
using SilkBound.Types;
using SilkBound.Types.NetLayers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace SilkBound.Utils
{
    public class NetworkUtils
    {
        public static LocalWeaver LocalClient = null!;
        public static NetworkServer LocalServer = null!;
        public static PacketHandler LocalPacketHandler = null!;
        public static ClientPacketHandler? ClientPacketHandler => LocalPacketHandler as ClientPacketHandler;
        public static ServerPacketHandler? ServerPacketHandler => LocalPacketHandler as ServerPacketHandler;
        public static Authority LocalAuthority => IsServer ? Authority.Server : Authority.Client; // ts so miniscule im not even putting it in the commit notes
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
                return (LocalConnection?.IsConnected ?? false) || Server.CurrentServer != null;
            }
        }

        public static event Action? Connected;

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
            LocalClient = LocalClient ?? new LocalWeaver(name, connection);

            Server.CurrentServer = new Server(LocalConnection);
            NetworkUtils.LocalConnection!.Send(new HandshakePacket(NetworkUtils.LocalClient!.ClientID, NetworkUtils.LocalClient!.ClientName));

            Logger.Debug("Connection Completed:", connection.GetType().Name, name, LocalClient.ClientID);

            Connected?.Invoke();

            return LocalClient;
        }
        public static void Disconnect(string reason="Unspecified")
        {
            //if (IsConnected)
                //SendPacket(new ClientDisconnectionPacket());
        }

        public static bool IsPacketThread()
        {
            StackTrace trace = new StackTrace(true);
            StackFrame[] frames = trace.GetFrames() ?? Array.Empty<StackFrame>();

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
    }
}
