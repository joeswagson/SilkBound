using HutongGames.PlayMaker.Actions;
using MelonLoader.Utils;
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

namespace SilkBound.Managers {
    public struct ConnectionRequest {
        // target
        public string Address;
        public int? Port;

        // connection process
        public bool InProgress;
        public bool Succeeded;
        public bool HandshakeFulfilled;
        public HandshakePacket? Handshake;

        // returned types
        public Weaver Client;
        public Server Server;

        // network layer
        public NetworkingLayer NetworkLayer;
        public NetworkConnection Connection;
    }
    public class ConnectionManager {
        public static ConnectionRequest Promise(NetworkingLayer networkingLayer, string ip, int? port = null) => new() {
            Address = ip,
            Port = port,

            InProgress = true,
            Succeeded = false,
            HandshakeFulfilled = false,

            NetworkLayer = networkingLayer,
        };
        public enum ConnectionError {
            NONE = -1,

            UNKNOWN = 0,
            TIMEOUT = 1,

            BANNED = 2,
            WHITELIST = 4,

            BADNAME = 5,
            BADGUID = 6,

            NULLTOKEN = 7,
            BADTOKEN = 8,
            EXPIREDTOKEN = 9,
            SPOOFEDTOKEN = 10,
        }
        private static string FormatError(params object[] reasons) => $"Connection Failed: {Logger.Msg}";
        private static void ConnectionFailed(ConnectionRequest request, ConnectionError error)
        {
            Logger.Warn(error switch {
                ConnectionError.NONE => FormatError("No error to report. This message is unintended behaviour."),
                ConnectionError.UNKNOWN => FormatError("Unknown."),
                ConnectionError.TIMEOUT => FormatError("Timed out."),
                ConnectionError.BANNED => FormatError("Banned from server."),
                ConnectionError.WHITELIST => FormatError("You are not whitelisted."),
                ConnectionError.BADNAME => FormatError("Invalid client name."),
                ConnectionError.BADGUID => FormatError("Invalid client ID."),
                ConnectionError.NULLTOKEN => FormatError("Authorization token unavailable."),
                ConnectionError.BADTOKEN => FormatError("Authorization token malformed."),
                ConnectionError.EXPIREDTOKEN => FormatError("Authorization expired."),
                ConnectionError.SPOOFEDTOKEN => FormatError("Authorization was spoofed."),
                _ => FormatError("Failed to resolve error message for", error.ToString())
            });
        }
        private static void ConnectionCompleted(ConnectionRequest request, Weaver result)
        {
            request.InProgress = false;

            var conn = result.Connection;
            request.Client = result;
            request.Connection = conn;

            if (conn.IsConnected && conn.PacketHandler is ClientPacketHandler cHandler)
            {
                cHandler.HandshakeFulfilled += () => {
                    request.HandshakeFulfilled = true;
                };

                cHandler.HandshakePacketFulfilled += (original) => {
                    request.Handshake = original;
                };

                request.Succeeded = true;
            } else
            {
                ConnectionFailed(request, ConnectionError.UNKNOWN);
            }
        }
        private static async Task ProcessConnectionTask(ConnectionRequest request, Task<Weaver> @async)
        {
            if (async.IsCompleted)
            {
                ConnectionCompleted(request, async.Result);
                return;
            }

            if (await Task.WhenAny(async, Task.Delay(SilkConstants.CONNECTION_TIMEOUT)) == async)
                ConnectionCompleted(request, async.Result);
            else
                ConnectionFailed(request, ConnectionError.TIMEOUT);
        }
        public static async Task<ConnectionRequest> Client(NetworkingLayer networkingLayer, string ip, int? port = null, string? name = null)
        {
            name ??= ModMain.Config.Username;
            ConnectionRequest request = Promise(networkingLayer, ip, port);

            switch (networkingLayer)
            {
                case NetworkingLayer.TCP:
                    await ProcessConnectionTask(request, NetworkUtils.ConnectTCPAsync(ip, name, port));
                    break;
                case NetworkingLayer.Steam:
                    await ProcessConnectionTask(request, NetworkUtils.ConnectP2PAsync(ulong.Parse(ip), name));
                    break;
                case NetworkingLayer.NamedPipe:
                    await ProcessConnectionTask(request, NetworkUtils.ConnectPipeAsync(ip, name));
                    break;
            }

            return request;
        }

        public static void Server(NetworkingLayer networkingLayer)
        {

        }
    }
}
