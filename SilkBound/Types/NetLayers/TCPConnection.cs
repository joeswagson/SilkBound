using SilkBound.Network.Packets;
using SilkBound.Packets;
using SilkBound.Utils;
using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace SilkBound.Types.NetLayers
{
    public class TCPConnection : NetworkConnection
    {
        private readonly TcpClient _client;
        private readonly string _remoteId;
        private readonly bool _isServerSide;

        private NetworkStream? _stream;
        private CancellationTokenSource? _cts;
        private Task? _recvTask;

        public bool HasConnection { get; private set; }

        public TCPConnection(string host, int? port=null) : base(new ClientPacketHandler())
        {
            port = port ?? SilkConstants.PORT;
            _client = new TcpClient();
            _remoteId = host + ":" + port;
            _isServerSide = false;

            Connect(host, port);
        }

        internal TCPConnection(TcpClient client, string remoteId, bool isServerSide)
            : base(isServerSide ? new ServerPacketHandler() : new ClientPacketHandler())
        {
            _client = client;
            _remoteId = remoteId;
            _isServerSide = isServerSide;
            ConnectImpl(null!, null); // i should make them null by default (especially since the 2nd param is already supposed to be) for bs like this but it looks cool on my ide with the minecraft font lol
        }

        public override void ConnectImpl(string host, int? port)
        {
            try
            {
                if (!_client.Connected && host != null && port != null)
                    _client.Connect(host, port.Value);

                _stream = _client.GetStream();

                _cts = new CancellationTokenSource();
                _recvTask = Task.Run(() => ReceiveLoopAsync(_cts.Token), _cts.Token);

                HasConnection = true;
                Logger.Msg($"[TCPConnection] Connected to {_remoteId}");
            }
            catch (Exception ex)
            {
                Logger.Error($"[TCPConnection] ConnectImpl failed: {ex}");
            }
        }

        private async Task ReceiveLoopAsync(CancellationToken ct)
        {
            byte[] buffer = new byte[SilkConstants.PACKET_BUFFER];

            try
            {
                while (!ct.IsCancellationRequested && _stream != null)
                {
                    int read = await _stream.ReadAsync(buffer, 0, buffer.Length, ct).ConfigureAwait(false);
                    if (read <= 0) break;

                    byte[] data = new byte[read];
                    Array.Copy(buffer, data, read);

                    try
                    {
                        HandleIncoming(data);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"[TCPConnection] Error handling packet from {_remoteId}: {ex}");
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Logger.Warn($"[TCPConnection] Receive loop ended for {_remoteId}: {ex.Message}");
            }
            finally
            {
                Disconnect();
            }
        }

        public override void Disconnect()
        {
            try
            {
                HasConnection = false;

                _cts?.Cancel();
                try { _recvTask?.Wait(500); } catch { }
                _cts?.Dispose();
                _cts = null;

                _stream?.Close();
                _client.Close();
            }
            catch (Exception e)
            {
                Logger.Warn($"[TCPConnection] Disconnect error: {e}");
            }
        }

        public override void Initialize()
        {
        }

        public override void Send(Packet packet)
        {
            if (!HasConnection || _stream == null)
            {
                Logger.Warn($"[TCPConnection] Send failed, no stream for {_remoteId}");
                return;
            }

            byte[]? data = PacketProtocol.PackPacket(packet);
            if (data == null) return;

            try
            {
                _stream.Write(data, 0, data.Length);
                _stream.Flush();
            }
            catch (Exception ex)
            {
                Logger.Warn($"[TCPConnection] Send error to {_remoteId}: {ex}");
            }
        }

        internal void HandleIncoming(byte[] data)
        {
            try
            {
                HandlePacket(data);
            }
            catch (Exception ex)
            {
                Logger.Error($"[TCPConnection] HandleIncoming failed for {_remoteId}: {ex}");
            }
        }

        public string RemoteId => _remoteId;
    }
}
