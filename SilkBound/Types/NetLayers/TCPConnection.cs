using SilkBound.Network.Packets;
using SilkBound.Network.Packets.Handlers;
using SilkBound.Utils;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using SilkBound.Network.Packets.Impl.Communication;
using System.Runtime.CompilerServices;
using SilkBound.Managers;

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

        public TCPConnection(string host, int? port = null) : base(new ClientPacketHandler(), host, port)
        {
            port = port ?? SilkConstants.PORT;
            _client = new TcpClient();
            _remoteId = host + ":" + port;
            _isServerSide = false;

            //Connect(host, port);
        }

        internal TCPConnection(TcpClient client, string remoteId, bool isServerSide, PacketHandler handler)
            : base(handler)
        {
            _client = client;
            _remoteId = remoteId;
            _isServerSide = isServerSide;
            _ = ConnectImpl(null!, null); // already running in async context
        }

        public override async Task ConnectImpl(string host, int? port)
        {
            try
            {
                if (host != null && (_client.Client == null || !_client.Client.Connected))
                    await _client.ConnectAsync(host, port ?? ConnectionManager.Port);

                _stream = _client.GetStream();

                _cts = new CancellationTokenSource();
                _recvTask = ReceiveLoopAsync(_cts.Token);

                HasConnection = true;
                Logger.Msg($"[TCPConnection] Connected to {_remoteId}");
#if SERVER
                Send(new HandshakePacket(Guid.Empty, "server"));
#else
                Send(new HandshakePacket(NetworkUtils.LocalClient.ClientID, NetworkUtils.LocalClient.ClientName));
#endif
            }
            catch (Exception ex)
            {
                Logger.Error($"[TCPConnection] ConnectImpl failed: {ex}");
            }
        }

        private async Task ReceiveLoopAsync(CancellationToken ct)
        {
            byte[] buffer = new byte[SilkConstants.PACKET_BUFFER];
            MemoryStream recvBuffer = new();

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
            catch (OperationCanceledException)
            {
            }
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
                try
                {
                    _recvTask?.Wait(500);
                }
                catch
                {
                }

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

        public override void Send(byte[] packetData)
        {
            if (packetData == null) return;
            if (!HasConnection || _stream == null)
            {
                Logger.Warn($"[TCPConnection] Send failed, no stream for {_remoteId}");
                return;
            }

            try
            {
                _stream.WriteAsync(packetData, 0, packetData.Length);
                //_stream.Flush();
            } catch (Exception ex)
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
