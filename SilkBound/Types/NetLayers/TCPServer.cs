using MelonLoader.TinyJSON;
using SilkBound.Network.Packets;
using SilkBound.Network.Packets.Handlers;
using SilkBound.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace SilkBound.Types.NetLayers
{
    public class TCPServer : NetworkServer
    {
        private readonly Dictionary<string, TCPConnection> _connections = new();
        private readonly object _connLock = new();

        private TcpListener? _listener;
        private CancellationTokenSource? _cts;
        private Task? _acceptTask;

        public override bool IsConnected => _connections.Count > 0;
        public TCPServer(string host, int? port = null) : base(new ServerPacketHandler())
        {
            Connect(host, port ?? SilkConstants.PORT);
        }
        public override void ConnectImpl(string host, int? port)
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
            _acceptTask = Task.Run(() => AcceptLoopAsync(_cts.Token), _cts.Token);
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
                            var conn = new TCPConnection(client, key, true);
                            _connections[key] = conn;
                            Logger.Msg($"[TCPServer] Connection accepted from {key}");
                        }
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
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
            }
            catch (Exception ex)
            {
                Logger.Warn($"[TCPServer] Disconnect error: {ex}");
            }

            Logger.Msg("[TCPServer] Disconnected.");
        }

        public override void Initialize()
        {
        }

        public override void Send(Packet packet)
        {
            byte[]? data = PacketProtocol.PackPacket(packet);
            if (data == null) return;

            lock (_connLock)
            {
                foreach (var conn in _connections.Values)
                {
                    try { conn.Send(packet); }
                    catch (Exception e) { Logger.Warn($"[TCPServer] Failed send to {conn.RemoteId}: {e}"); }
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

        public override void SendIncluding(Packet packet, List<NetworkConnection> include)
        {
            byte[]? data = PacketProtocol.PackPacket(packet);
            if (data == null) return;

            lock (_connLock)
            {
                foreach (var conn in _connections.Values)
                {
                    if (include.Contains(conn))
                        try { conn.Send(packet); }
                        catch (Exception e) { Logger.Warn($"[TCPServer] Failed send to {conn.RemoteId}: {e}"); }
                }
            }
        }

        public override void SendExcluding(Packet packet, List<NetworkConnection> exclude)
        {
            byte[]? data = PacketProtocol.PackPacket(packet);
            if (data == null) return;

            lock (_connLock)
            {
                foreach (var conn in _connections.Values)
                {
                    if (!exclude.Contains(conn))
                        try { conn.Send(packet); }
                        catch (Exception e) { Logger.Warn($"[TCPServer] Failed send to {conn.RemoteId}: {e}"); }
                }
            }
        }
    }
}
