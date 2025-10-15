using SilkBound.Network.Packets;
using SilkBound.Network.Packets.Handlers;
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

        public override bool IsConnected => _client != null && _client.Connected;
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
                _recvTask = ReceiveLoopAsync(_cts.Token);

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
            MemoryStream recvBuffer = new MemoryStream();

            try
            {
                while (!ct.IsCancellationRequested && _stream != null)
                {
                    int read = await _stream.ReadAsync(buffer, 0, buffer.Length, ct).ConfigureAwait(false);
                    if (read <= 0) break;

                    // append new data
                    recvBuffer.Position = recvBuffer.Length;
                    recvBuffer.Write(buffer, 0, read);

                    // process all complete packets
                    while (true)
                    {
                        recvBuffer.Position = 0;
                        if (recvBuffer.Length < 4) break; // not enough for header

                        int packetLength;
                        using (var reader = new BinaryReader(recvBuffer, System.Text.Encoding.UTF8, true))
                        {
                            packetLength = reader.ReadInt32();
                        }

                        if (recvBuffer.Length - 4 < packetLength)
                            break; // incomplete packet

                        // extract packet
                        byte[] packetData = new byte[packetLength];
                        recvBuffer.Position = 4;
                        recvBuffer.Read(packetData, 0, packetLength);

                        try
                        {
                            HandleIncoming(packetData);
                        }
                        catch (Exception ex)
                        {
                            Logger.Error($"[TCPConnection] Error handling packet from {_remoteId}: {ex}");
                        }

                        // compact leftover bytes
                        long remaining = recvBuffer.Length - (4 + packetLength);
                        if (remaining > 0)
                        {
                            byte[] leftover = new byte[remaining];
                            recvBuffer.Read(leftover, 0, (int)remaining);
                            recvBuffer.SetLength(0);
                            recvBuffer.Write(leftover, 0, leftover.Length);
                        }
                        else
                        {
                            recvBuffer.SetLength(0);
                        }
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
