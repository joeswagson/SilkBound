using HutongGames.PlayMaker.Actions;
using Newtonsoft.Json;
using SilkBound.Lib.DbgRender.Renderers;
using SilkBound.Network;
using SilkBound.Network.Packets;
using SilkBound.Network.Packets.Handlers;
using SilkBound.Network.Packets.Impl.Communication;
using SilkBound.Types;
using SilkBound.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using _Server = SilkBound.Types.Server;

namespace SilkBound.Managers {
    public class ServerStart {
        // target
        public string? Address;
        public int? Port;

        // connection process
        public bool InProgress;
        public bool Succeeded;

        // returned types

        public Weaver? Client;
        public Server? Server;

        // network layer
        public NetworkingLayer NetworkLayer;
        public NetworkServer? Connection;

        public void Dump()
        {
            Logger.Msg("Address:", Address);
            Logger.Msg("Port:", Port);
            Logger.Msg("InProgress:", InProgress);
            Logger.Msg("Succeeded:", Succeeded);
            Logger.Msg("Client:", Client);
            Logger.Msg("Server:", Server);
            Logger.Msg("NetworkLayer:", NetworkLayer);
            Logger.Msg("Connection:", Connection);
        }
    }
    public class ConnectionRequest {
        // target
        public string? Address;
        public int? Port;

        // connection process
        public bool InProgress;
        public bool Succeeded;
        public bool HandshakeFulfilled;
        public HandshakePacket? Handshake;

        // returned types
        public Weaver? Client;
        public Server? Server;

        // network layer
        public NetworkingLayer NetworkLayer;
        public NetworkConnection? Connection;

        public void Dump()
        {
            Logger.Msg("Address:", Address);
            Logger.Msg("Port:", Port);
            Logger.Msg("InProgress:", InProgress);
            Logger.Msg("Succeeded:", Succeeded);
            Logger.Msg("HandshakeFulfilled:", HandshakeFulfilled);
            Logger.Msg("Handshake:", Handshake);
            Logger.Msg("Client:", Client);
            Logger.Msg("Server:", Server);
            Logger.Msg("NetworkLayer:", NetworkLayer);
            Logger.Msg("Connection:", Connection);
        }
    }
    public readonly struct Error {
        public readonly int Code;
        public readonly string Message;
        public readonly string Tag;
        public readonly string Formatted;
        private Error(int code, string message, string tag = "Error")
        {
            Code = code;
            Message = message;
            Tag = tag;
            Formatted = $"{Tag} ({code}): {Message}";
        }

        // 0-100 Reserved Codes
        public static readonly Error NONE = new(0, "No error.");
        public static readonly Error UNKNOWN = new(1, "Unknown error.");
        public static readonly Error BINGUS = new(2, "bingus");

        // 100-199 Generic Codes
        public static readonly Error TIMEOUT = new(100, "Timed out.");
        public static readonly Error BADNAME = new(101, "Invalid name.");
        public static readonly Error BADGUID = new(102, "Invalid GUID.");
        public static readonly Error NULLTOKEN = new(103, "Invalid token.");
        public static readonly Error BADTOKEN = new(104, "Malformed token.");
        public static readonly Error EXPIREDTOKEN = new(105, "Expired token.");
        public static readonly Error SPOOFEDTOKEN = new(106, "Spoofed token.");

        // 200-299 Client Codes
        public static class Client {
            public static readonly Error BANNED = new(200, "You are banned.");
            public static readonly Error WHITELIST = new(201, "You haven't been whitelisted.");
            public static readonly Error AMBIDEXTROUS = new(201, "Client attemped a double handshake.");
        }

        // 300-399 Server Codes
        public static class Server {
            public static readonly Error BINDFAIL = new(300, "Failed to bind to specified server host.");
            public static readonly Error PORTFAIL = new(301, "Server port is unavailable.");
        }
    }


    public class ConnectionManager {
        public static ushort Port => Silkbound.Config.Port;
        internal static void UpdateMenuStatus(ConnectionStatus status)
        {
            Silkbound.ConnectionMenu?.SetStatus(status);
        }

        public static void Disconnect(string reason = "Unspecified.")
        {
            NetworkUtils.Disconnect(reason);
        }

        #region Server

        public static ServerStart PromiseStartup(NetworkingLayer networkingLayer, string? ip = null, int? port = null) => new() {
            Address = ip,
            Port = port,

            InProgress = true,
            Succeeded = false,

            NetworkLayer = networkingLayer,
        };
        private static void StartupFailed(ServerStart request, Error error)
        {
            request.Succeeded = false;

            UpdateMenuStatus(ConnectionStatus.Disconnected);
            Logger.Warn(error.Formatted);
        }
        private static void StartupCompleted(ServerStart request, Server result)
        {
            request.InProgress = false;

            var conn = (NetworkServer) result.Connection;
            request.Server = result;
            request.Client = result.Host;
            request.Connection = conn;

            NetworkStatsRenderer.ObjectSafeAssignAll(conn);

            if (conn.IsConnected)
            {
                request.Succeeded = true;

                UpdateMenuStatus(ConnectionStatus.Connected);
            } else
            {
                StartupFailed(request, Error.UNKNOWN);
            }
        }
        private static async Task ProcessStartupTask(ServerStart request, Task<_Server> @async)
        {
            if (async.IsCompleted)
            {
                StartupCompleted(request, async.Result);
                return;
            }

            try
            {
                if (await Task.WhenAny(Task.Delay(SilkConstants.CONNECTION_TIMEOUT), async) == async)
                    StartupCompleted(request, async.Result);
                else
                    StartupFailed(request, Error.TIMEOUT);
            } catch (Exception ex)
            {
                Logger.Error(ex);
                Logger.Error(ex.Message);
                Logger.Error(ex.StackTrace);
            }
        }
        public static async Task<ServerStart> Server()
        {
            var config = Silkbound.Config;
            return await Server(config.NetworkLayer, config.ConnectIP, config.Port, config.Username);
        }
        public static async Task<ServerStart> Server(NetworkingLayer? networkingLayer = null, string? ip = null, int? port = null, string? name = null)
        {
            UpdateMenuStatus(ConnectionStatus.Connecting);

            networkingLayer ??= Silkbound.Config.NetworkLayer;
            ip ??= Silkbound.Config.HostIP;
            port ??= Silkbound.Config.Port;
            name ??= Silkbound.Config.Username;

            ServerStart request = PromiseStartup(networkingLayer.Value, ip, port);
            Logger.Msg("handling layer");
            switch (networkingLayer.Value)
            {
                case NetworkingLayer.TCP:
                    await ProcessStartupTask(request, _Server.ConnectTCPAsync(ip!, name, port));
                    break;
                case NetworkingLayer.Steam:
                    await ProcessStartupTask(request, _Server.ConnectP2PAsync(name));
                    break;
                case NetworkingLayer.NamedPipe:
                    await ProcessStartupTask(request, _Server.ConnectPipeAsync(ip!, name));
                    break;
            }
            Logger.Msg("handled layer");

            request.InProgress = false;

            return request;
        }

        #endregion

        #region Client

        public static ConnectionRequest Promise(NetworkingLayer networkingLayer, string ip, int? port = null) => new() {
            Address = ip,
            Port = port,

            InProgress = true,
            Succeeded = false,
            HandshakeFulfilled = false,

            NetworkLayer = networkingLayer,
        };
        public static void ConnectionFailed(ConnectionRequest request, Error? error = null)
        {
            request.Succeeded = false;
            if (NetworkUtils.Connected)
                NetworkUtils.Disconnect();

            UpdateMenuStatus(ConnectionStatus.Disconnected);
            Logger.Warn((error ?? Error.UNKNOWN).Formatted);
        }
        public static void ConnectionCompleted(ConnectionRequest request, Weaver result)
        {
            request.InProgress = false;

            var conn = result.Connection;
            request.Client = result;
            request.Connection = conn;

            NetworkStatsRenderer.ObjectSafeAssignAll(conn);

            if (conn.IsConnected && conn.PacketHandler is ClientPacketHandler cHandler)
            {
                cHandler.HandshakeFulfilled += () => {
                    request.HandshakeFulfilled = true;
                };

                cHandler.HandshakePacketFulfilled += (original) => {
                    request.Handshake = original;
                };

                request.Succeeded = true;

                UpdateMenuStatus(ConnectionStatus.Connected);
            } else
            {
                ConnectionFailed(request, Error.UNKNOWN);
            }
        }
        private static async Task ProcessConnectionTask(ConnectionRequest request, Task<Weaver> async)
        {
            if (async.IsCompleted)
            {
                ConnectionCompleted(request, async.Result);
                return;
            }

            if (await Task.WhenAny(async, Task.Delay(SilkConstants.CONNECTION_TIMEOUT)) == async)
                ConnectionCompleted(request, async.Result);
            else
                ConnectionFailed(request, Error.TIMEOUT);
        }
        public static async Task<ConnectionRequest> Client(NetworkingLayer? networkingLayer = null, string? ip = null, int? port = null, string? name = null)
        {
            var config = Silkbound.Config;
            return await Client(networkingLayer ?? config.NetworkLayer, ip ?? config.ConnectIP, port ?? config.Port, name ?? config.Username);
        }
        public static async Task<ConnectionRequest> Client(NetworkingLayer networkingLayer, string ip, int? port = null, string? name = null)
        {
            UpdateMenuStatus(ConnectionStatus.Connecting);

            name ??= Silkbound.Config.Username;
            ConnectionRequest request = Promise(networkingLayer, ip, port);

            switch (networkingLayer)
            {
                case NetworkingLayer.TCP:
                    await ProcessConnectionTask(request, NetworkUtils.ConnectTCPAsync(request, ip, name, port));
                    break;
                case NetworkingLayer.Steam:
                    await ProcessConnectionTask(request, NetworkUtils.ConnectP2PAsync(request, ulong.Parse(ip), name));
                    break;
                case NetworkingLayer.NamedPipe:
                    await ProcessConnectionTask(request, NetworkUtils.ConnectPipeAsync(request, ip, name));
                    break;
            }

            request.InProgress = false;

            return request;
        }

        #endregion
    }
}
