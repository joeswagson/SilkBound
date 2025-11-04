using SilkBound.Network.Packets;
using SilkBound.Utils;
using Steamworks;
using System;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using SilkBound.Network.Packets.Handlers;

namespace SilkBound.Types.NetLayers
{
    public class SteamConnection : NetworkConnection
    {
        internal CSteamID _remoteId;
        internal bool _isServerSide;

        internal CancellationTokenSource? _receiveCts;
        internal Task? _receiveTask;

        public bool HasConnection { get; private set; } = false;
        public override bool IsConnected => HasConnection;

        public SteamConnection(string host) : base(new ClientPacketHandler())
        {
            Connect(host, null);
        }

        public SteamConnection(CSteamID remoteId, bool isServerSide) : base(isServerSide ? new ServerPacketHandler() : new ClientPacketHandler())
        {
            _remoteId = remoteId;
            _isServerSide = isServerSide;
        }

        public override async Task ConnectImpl(string host, int? port)
        {
            if (!ulong.TryParse(host, out ulong steamIdUlong))
            {
                Logger.Warn($"[SteamConnection] ConnectImpl: invalid host (not a ulong steam id): '{host}'");
                return;
            }

            _remoteId = new CSteamID(steamIdUlong);
            _isServerSide = false;

            SteamNetworking.AcceptP2PSessionWithUser(_remoteId);
            Logger.Msg($"[SteamConnection] ConnectImpl: session accepted for {_remoteId}");

            _receiveCts = new CancellationTokenSource();
            _receiveTask = Task.Run(() => ClientReceiveLoopAsync(_receiveCts.Token), _receiveCts.Token);

            HasConnection = true;
        }

        private async Task ClientReceiveLoopAsync(CancellationToken ct)
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
                                if (sender == _remoteId)
                                {
                                    try
                                    {
                                        using var ms = new MemoryStream(buffer);
                                        using var br = new BinaryReader(ms);

                                        int length = br.ReadInt32(); // strip length prefix
                                        byte[] payload = br.ReadBytes(length);

                                        HandleIncoming(payload);
                                    }
                                    catch (Exception ex)
                                    {
                                        Logger.Error($"[SteamConnection] Error handling packet from {sender}: {ex}");
                                    }
                                }
                                else
                                {
                                    Logger.Warn($"[SteamConnection] Received packet from unexpected sender {sender} (expected {_remoteId}) — dropping.");
                                }
                            }
                        }
                    }
                    catch (Exception inner)
                    {
                        Logger.Warn($"[SteamConnection] client receive loop read error: {inner}");
                    }

                    await Task.Delay(10, ct).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Logger.Error($"[SteamConnection] ClientReceiveLoop fatal: {ex}");
            }
        }


        public override void Disconnect()
        {
            if (NetworkUtils.LocalConnection != this) // prevent morons (me later) desyncing other players locally
            {
                Logger.Error("You do not have permission (or ability) to disconnect other players.");
                return;
            }

            try
            {
                HasConnection = false;

                if (_receiveCts != null)
                {
                    _receiveCts.Cancel();
                    try { _receiveTask?.Wait(500); } catch { }
                    _receiveCts.Dispose();
                    _receiveCts = null;
                }

                SteamNetworking.CloseP2PSessionWithUser(_remoteId);
            }
            catch (Exception e)
            {
                Logger.Warn($"[SteamConnection] Disconnect error: {e}");
            }
        }

        public override void Initialize()
        {

        }

        public override void Send(byte[] packetData)
        {
            if (_remoteId == CSteamID.Nil)
            {
                Logger.Warn("[SteamConnection] Send: remote id is not set.");
                return;
            }

            bool ok = SteamNetworking.SendP2PPacket(_remoteId, packetData, (uint) packetData.Length, EP2PSend.k_EP2PSendReliable);
            if (!ok) Logger.Warn($"[SteamConnection] Send failed to {_remoteId}");
        }

        internal void HandleIncoming(byte[] data)
        {
            try
            {
                HandlePacket(data);
            }
            catch (Exception ex)
            {
                Logger.Error($"[SteamConnection] HandleIncoming failed for {_remoteId}: {ex}");
            }
        }

        public CSteamID RemoteId => _remoteId;
    }
}
