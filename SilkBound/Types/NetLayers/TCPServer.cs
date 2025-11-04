using SilkBound.Network.Packets;
using SilkBound.Network.Packets.Handlers;
using SilkBound.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace SilkBound.Types.NetLayers {
    public class TCPServer : NetworkServer {
        private readonly Dictionary<string, TCPConnection> _connections = [];
        private readonly object _connLock = new();

        private TcpListener? _listener;
        private CancellationTokenSource? _cts;
        private Task? _acceptTask;
        private PacketHandler _handler;

        public override bool IsConnected => _connections.Count > 0;
        public TCPServer(string host, PacketHandler handler, int? port = null) : base(handler)
        {
            //Connect(host, port ?? SilkConstants.PORT);
            _handler = handler;
        }
        public override async Task ConnectImpl(string host, int? port)
        {
            if (port == null)
            {
                Logger.Error("[TCPServer] Port must be provided.");
                return;
            }

            _listener = new TcpListener(IPAddress.Any, port.Value);
            _listener.Start();
            Logger.Msg($"[TCPServer] Listening on port {port.Value}...");

            _cts = new CancellationTokenSource();
            _acceptTask = AcceptLoopAsync(_cts.Token);
        }

        private async Task AcceptLoopAsync(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested && _listener != null)
                {
                    TcpClient client = await _listener.AcceptTcpClientAsync().ConfigureAwait(false);
                    string key = client.Client.RemoteEndPoint?.ToString() ?? Guid.NewGuid().ToString();

                    lock (_connLock)
                    {
                        if (!_connections.ContainsKey(key))
                        {
                            var conn = new TCPConnection(client, key, true, _handler);
                            _connections[key] = conn;
                            Logger.Msg($"[TCPServer] Connection accepted from {key}");
                        }
                    }
                }
            } catch (OperationCanceledException) { } catch (Exception ex)
            {
                Logger.Warn($"[TCPServer] AcceptLoop error: {ex}");
            }
        }


        public override void Disconnect()
        {
            try
            {
                _cts?.Cancel();
                try { _acceptTask?.Wait(500); } catch { }
                _cts?.Dispose();
                _cts = null;

                _listener?.Stop();
                _listener = null;

                lock (_connLock)
                {
                    foreach (var conn in _connections.Values)
                        conn.Disconnect();

                    _connections.Clear();
                }
            } catch (Exception ex)
            {
                Logger.Warn($"[TCPServer] Disconnect error: {ex}");
            }

            Logger.Msg("[TCPServer] Disconnected.");
        }

        public override void Initialize()
        {
        }

        public override void Send(byte[] packetData)
        {
            lock (_connLock)
            {
                foreach (var conn in _connections.Values)
                {
                    try { conn.Send(packetData); } catch (Exception e) { Logger.Warn($"[TCPServer] Failed send to {conn.RemoteId}: {e}"); }
                }
            }
        }

        public IReadOnlyCollection<string> GetPlayerList()
        {
            lock (_connLock)
            {
                return new List<string>(_connections.Keys).AsReadOnly();
            }
        }

        public IReadOnlyCollection<TCPConnection> GetConnections()
        {
            lock (_connLock)
            {
                return new List<TCPConnection>(_connections.Values).AsReadOnly();
            }
        }

        public override void SendIncluding(Packet packet, IEnumerable<NetworkConnection> include)
        {
            lock (_connLock)
            {
                if (include.Count() == 0) return; // dont pack if no targets

                byte[]? data = PacketProtocol.PackPacket(packet);
                if (data == null) return;

                foreach (TCPConnection conn in include)
                {
                    Logger.Msg("Sending", packet.GetType().Name, "to", conn.RemoteId);
                    try { conn.Send(data); } catch (Exception e) { Logger.Warn($"[TCPServer] Failed send to {conn.RemoteId}: {e}"); }
                }
            }
        }

        public override void SendExcluding(Packet packet, IEnumerable<NetworkConnection> exclude)
        {
            lock (_connLock)
            {
                NetworkConnection[] targets = [.. _connections.Values.Except(exclude)];
                if (targets.Length == 0) return; // dont pack if no targets

                byte[]? data = PacketProtocol.PackPacket(packet);
                if (data == null) return;

                foreach (TCPConnection conn in targets)
                {
                    try { conn.Send(data); } catch (Exception e) { Logger.Warn($"[TCPServer] Failed send to {conn.RemoteId}: {e}"); }
                }
            }
        }
    }
}
