using MelonLoader.TinyJSON;
using SilkBound.Network.Packets;
using SilkBound.Network.Packets.Handlers;
using SilkBound.Utils;
using Steamworks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SilkBound.Types.NetLayers {
    public class SteamServer : NetworkServer {
        private readonly Dictionary<CSteamID, SteamConnection> _connections = [];
        private readonly object _connLock = new();

        private Callback<P2PSessionRequest_t>? _p2pSessionRequest;
        private Callback<P2PSessionConnectFail_t>? _p2pSessionFail;

        private CancellationTokenSource? _recvCts;
        private Task? _recvTask;
        public override bool IsConnected => _connections.Count > 0;

        public SteamServer() : base(new ServerPacketHandler())
        {
            //Connect(string.Empty, null);
        }

        public override async Task ConnectImpl(string host, int? port)
        {
            Logger.Msg("[SteamServer] Starting up SteamServer...");

            _p2pSessionRequest = Callback<P2PSessionRequest_t>.Create(OnP2PSessionRequest);
            _p2pSessionFail = Callback<P2PSessionConnectFail_t>.Create(OnP2PSessionFail);

            _recvCts = new CancellationTokenSource();
            _ = ReceiveLoopAsync(_recvCts.Token);
            Logger.Msg("[SteamServer] Ready for incoming Steam P2P connections.");
        }

        private void OnP2PSessionRequest(P2PSessionRequest_t req)
        {
            Logger.Msg($"[SteamServer] Incoming session request from {req.m_steamIDRemote}");

            if (!SteamNetworking.AcceptP2PSessionWithUser(req.m_steamIDRemote))
            {
                Logger.Warn($"[SteamServer] Failed to accept session with {req.m_steamIDRemote}");
                return;
            }

            lock (_connLock)
            {
                if (!_connections.ContainsKey(req.m_steamIDRemote))
                {
                    var conn = new SteamConnection(req.m_steamIDRemote, true, Stats);
                    _connections[req.m_steamIDRemote] = conn;
                    Logger.Msg($"[SteamServer] Connection object created for {req.m_steamIDRemote}");
                }
            }
        }

        private void OnP2PSessionFail(P2PSessionConnectFail_t fail)
        {
            Logger.Warn($"[SteamServer] P2P session failed with {fail.m_steamIDRemote} ({fail.m_eP2PSessionError})");
            lock (_connLock)
            {
                _connections.Remove(fail.m_steamIDRemote);
            }
        }

        private async Task ReceiveLoopAsync(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    try
                    {
                        while (SteamNetworking.IsP2PPacketAvailable(out uint msgSize))
                        {
                            byte[] buffer = new byte[msgSize];
                            if (SteamNetworking.ReadP2PPacket(buffer, msgSize, out uint bytesRead, out CSteamID sender))
                            {
                                SteamConnection? conn = null;
                                lock (_connLock)
                                {
                                    _connections.TryGetValue(sender, out conn);
                                }

                                if (conn == null)
                                {
                                    if (SteamNetworking.AcceptP2PSessionWithUser(sender))
                                    {
                                        lock (_connLock)
                                        {
                                            conn = new SteamConnection(sender, true, Stats);
                                            _connections[sender] = conn;
                                            Logger.Msg($"[SteamServer] Auto-accepted and created connection for {sender}");
                                        }
                                    } else
                                    {
                                        Logger.Warn($"[SteamServer] Received packet from unknown sender {sender} and AcceptP2PSessionWithUser returned false — dropping.");
                                        continue;
                                    }
                                }

                                try
                                {
                                    using var ms = new MemoryStream(buffer);
                                    using var br = new BinaryReader(ms);

                                    int length = br.ReadInt32(); // strip prefix
                                    byte[] payload = br.ReadBytes(length);

                                    HandlePacket(payload);
                                } catch (Exception ex)
                                {
                                    Logger.Error($"[SteamServer] Error dispatching packet from {sender}: {ex}");
                                }
                            }
                        }
                    } catch (Exception inner)
                    {
                        Logger.Warn($"[SteamServer] Receive loop read error: {inner}");
                    }

                    await Task.Delay(10, ct).ConfigureAwait(false);
                }
            } catch (OperationCanceledException) { } catch (Exception ex)
            {
                Logger.Error($"[SteamServer] ReceiveLoop fatal: {ex}");
            }
        }


        public override void Dispose()
        {
            try
            {
                if (_recvCts != null)
                {
                    _recvCts.Cancel();
                    try { _recvTask?.Wait(500); } catch { }
                    _recvCts.Dispose();
                    _recvCts = null;
                }

                lock (_connLock)
                {
                    foreach (var kv in _connections)
                    {
                        try { SteamNetworking.CloseP2PSessionWithUser(kv.Key); } catch (Exception e) { Logger.Warn($"Error closing session for {kv.Key}: {e}"); }
                    }
                    _connections.Clear();
                }

                _p2pSessionRequest?.Unregister();
                _p2pSessionFail?.Unregister();
                _p2pSessionRequest = null;
                _p2pSessionFail = null;
            } catch (Exception ex)
            {
                Logger.Warn($"[SteamServer] Disconnect error: {ex}");
            }

            Logger.Msg("[SteamServer] Disconnected.");
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
                    Logger.Warn($"[SteamServer] Failed send to {conn.RemoteId}: {e}");
                }
            }
        }


        public IReadOnlyCollection<CSteamID> GetPlayerList()
        {
            lock (_connLock)
            {
                return new List<CSteamID>(_connections.Keys).AsReadOnly();
            }
        }

        public IEnumerable<SteamConnection> GetConnections()
        {
            lock (_connLock)
            {
                return _connections.Values.Where(c => c != NetworkUtils.LocalConnection);
            }
        }

        public override async Task SendIncluding(Packet packet, IEnumerable<NetworkConnection> include)
        {
            var targets = GetConnections().Intersect(include);
            if (!targets.Any())
                return;

            byte[]? data = PacketProtocol.PackPacket(packet);
            if (data == null) return;

            foreach (SteamConnection conn in targets)
            {
                try { await conn.Send(data); } catch (Exception e) { Logger.Warn($"[SteamServer] Failed send to {conn.RemoteId}: {e}"); }
            }
        }

        public override async Task SendExcluding(Packet packet, IEnumerable<NetworkConnection> exclude)
        {
            var targets = GetConnections().Except(exclude);
            if (!targets.Any())
                return;

            byte[]? data = PacketProtocol.PackPacket(packet);
            if (data == null) return;

            foreach (SteamConnection conn in targets.Cast<SteamConnection>())
            {
                try { await conn.Send(data); } catch (Exception e) { Logger.Warn($"[SteamServer] Failed send to {conn.RemoteId}: {e}"); }
            }
        }
    }
}
