using SilkBound.Network.Packets;
using SilkBound.Packets;
using SilkBound.Packets.Impl;
using SilkBound.Utils;
using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SilkBound.Types.NetLayers
{
    public class NamedPipeConnection : NetworkConnection
    {
        public NamedPipeConnection(string host) : base(new ClientPacketHandler())
        {
            Connect(host, null);
        }

        public NamedPipeClientStream? Stream;
        public override bool IsConnected => Stream != null && Stream.IsConnected;

        private CancellationTokenSource? _recvCts;
        private Task? _recvTask;

        public override void ConnectImpl(string host, int? port)
        {
            Stream = new NamedPipeClientStream(
                ".", host, PipeDirection.InOut,
                PipeOptions.Asynchronous
            );
            Logger.Msg("Connecting to NamedPipeServer...");
            Stream.Connect();
            Logger.Msg("Connected to server!");

            _recvCts = new CancellationTokenSource();
            _recvTask = Task.Run(() => ReceiveLoopAsync(_recvCts.Token), _recvCts.Token);
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
                        byte[] data = new byte[read];
                        Array.Copy(buffer, data, read);
                        try { HandlePacket(data); }
                        catch (Exception ex)
                        {
                            Logger.Error($"NamedPipeConnection HandlePacket failed: {ex}");
                        }
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (IOException e)
            {
                Logger.Warn($"NamedPipeConnection receive loop ended: {e.Message}");
            }
            catch (Exception ex)
            {
                Logger.Error($"NamedPipeConnection receive loop fatal: {ex}");
            }
        }

        public override void Disconnect()
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
            }
            catch (Exception e)
            {
                Logger.Warn($"NamedPipeConnection Disconnect error: {e}");
            }
        }

        public override void Initialize()
        {

        }

        public override async void Send(Packet packet)
        {
            if (Stream == null || !Stream.IsConnected)
            {
                Logger.Warn("Stream was null or not connected.");
                return;
            }

            byte[]? data = PacketProtocol.PackPacket(packet);
            if (data == null) return;

            try
            {
                await Stream.WriteAsync(data, 0, data.Length);
                await Stream.FlushAsync();
            }
            catch (Exception e)
            {
                Logger.Warn($"NamedPipeConnection send error: {e.Message} {e.GetType().Name}");
            }
        }
    }
}
