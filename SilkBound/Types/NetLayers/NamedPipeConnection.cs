using SilkBound.Network.Packets;
using SilkBound.Network.Packets.Handlers;
using SilkBound.Utils;
using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace SilkBound.Types.NetLayers {
    public class NamedPipeConnection : NetworkConnection {
        public NamedPipeConnection(string host) : base(new ClientPacketHandler(), host)
        {
            //Connect(host, null);
        }

        public NamedPipeClientStream? Stream;
        public override bool IsConnected => Stream != null && Stream.IsConnected;

        private CancellationTokenSource? _recvCts;
        private Task? _recvTask;

        public override async Task ConnectImpl(string host, int? port)
        {
            Stream = new NamedPipeClientStream(
                ".", host, PipeDirection.InOut,
                PipeOptions.Asynchronous
            );
            Logger.Msg("Connecting to NamedPipeServer...");
            await Stream.ConnectAsync();
            Logger.Msg("Connected to server!");

            _recvCts = new CancellationTokenSource();
            _ = ReceiveLoopAsync(_recvCts.Token);
        }

        private async Task ReceiveLoopAsync(CancellationToken ct)
        {
            byte[] buffer = new byte[SilkConstants.PACKET_BUFFER];

            try
            {
                while (!ct.IsCancellationRequested && Stream!.IsConnected)
                {
                    int read = await Stream.ReadAsync(buffer, 0, buffer.Length, ct);
                    if (read > 0)
                    {
                        byte[] data = new byte[read - 4];
                        Array.Copy(buffer, 4, data, 0, read - 4);
                        try { HandlePacket(data); } catch (Exception ex)
                        {
                            Logger.Error($"NamedPipeConnection HandlePacket failed: {ex}");
                        }
                    }
                }
            } catch (OperationCanceledException) { } catch (IOException e)
            {
                Logger.Warn($"NamedPipeConnection receive loop ended: {e.Message}");
            } catch (Exception ex)
            {
                Logger.Error($"NamedPipeConnection receive loop fatal: {ex}");
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

                Stream?.Dispose();
                Stream = null;
            } catch (Exception e)
            {
                Logger.Warn($"NamedPipeConnection Disconnect error: {e}");
            }
        }

        public override void Initialize()
        {

        }

        protected override async Task Write(byte[] packetData)
        {
            if (Stream == null || !Stream.IsConnected)
            {
                Logger.Warn("Stream was null or not connected.");
                return;
            }

            try
            {
                await Stream.WriteAsync(packetData, 0, packetData.Length);
                await Stream.FlushAsync();
            } catch (Exception e)
            {
                Logger.Warn($"NamedPipeConnection send error: {e.Message} {e.GetType().Name}");
            }
        }
    }
}
