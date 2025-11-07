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
    public class TCPServer(string host, PacketHandler handler, int? port = null) : NetworkServer(handler, host, port) {
        private readonly Dictionary<string, TCPConnection> _connections = [];
        private readonly object _connLock = new();

        private TcpListener? _listener;
        private CancellationTokenSource? _cts;
        private Task? _acceptTask;
        private PacketHandler _handler = handler;

        public override bool IsConnected => _acceptTask != null;

        public override async Task ConnectImpl(string host, int? port)
        {
            await Task.Run(() => {
                if (port == null)
                {
                    Logger.Error("[TCPServer] Port must be provided.");
                    return;
                }

                _listener = new TcpListener(IPAddress.Parse(host), port.Value);
                _listener.Start();
                Logger.Msg($"[TCPServer] Listening on port {port.Value}...");
            });

            _cts = new CancellationTokenSource();
            _acceptTask = Task.Run(()=>AcceptLoopAsync(_cts.Token));

            return;
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
                            var conn = new TCPConnection(client, key, true, _handler, Stats);
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


        public override void Dispose()
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

        protected override async Task Write(byte[] packetData)
        {
            foreach (var conn in GetConnections())
            {
                try
                {
                    await conn.Send(packetData);
                } catch (Exception e)
                {
                    Logger.Warn($"[TCPServer] Failed send to {conn.RemoteId}: {e}");
                }
            }
        }

        public IEnumerable<string> GetPlayerList()
        {
            lock (_connLock)
            {
                return _connections.Keys;
            }
        }

        public IEnumerable<TCPConnection> GetConnections()
        {
            lock (_connLock)
			{
				//Logger.Msg([.. _connections.Values.Where(c => c != NetworkUtils.LocalConnection).Select(c => c.GetType().Name)]);
				return _connections.Values.Where(c => c != NetworkUtils.LocalConnection);
			}
        }

        public override async Task SendIncluding(Packet packet, IEnumerable<NetworkConnection> include)
        {
            if (include.Count() == 0) return; // dont pack if no targets

            byte[]? data = PacketProtocol.PackPacket(packet);
            if (data == null) return;

            foreach (TCPConnection conn in include)
            {
                Logger.Msg("Sending", packet.GetType().Name, "to", conn.RemoteId);
                try { await conn.Send(data); } catch (Exception e) { Logger.Warn($"[TCPServer] Failed send to {conn.RemoteId}: {e}"); }
            }
        }

        public override async Task SendExcluding(Packet packet, IEnumerable<NetworkConnection> exclude)
        {
            NetworkConnection[] targets = [.. GetConnections().Except(exclude)];
            if (targets.Length == 0) return; // dont pack if no targets

            byte[]? data = PacketProtocol.PackPacket(packet);
            if (data == null) return;

            foreach (TCPConnection conn in targets)
            {
                try { await conn.Send(data); } catch (Exception e) { Logger.Warn($"[TCPServer] Failed send to {conn.RemoteId}: {e}"); }
            }
        }
    }
}
