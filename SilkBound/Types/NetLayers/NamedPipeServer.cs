using SilkBound.Network.Packets;
using SilkBound.Network.Packets.Handlers;
using SilkBound.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading.Tasks;

namespace SilkBound.Types.NetLayers
{
    public class NamedPipeServer : NetworkServer
    {
        public NamedPipeServer(string host) : base(new ServerPacketHandler())
        {
            Connect(host, null);
        }

        public NamedPipeServerStream? Stream;
        public override bool IsConnected => Stream != null && Stream.IsConnected;

        public override void ConnectImpl(string host, int? port)
        {
            Stream = new NamedPipeServerStream(
                host,
                PipeDirection.InOut,
                1,
                PipeTransmissionMode.Message,
                PipeOptions.Asynchronous
            );

            Logger.Msg("NamedPipeServerStream object created.");
            Logger.Msg("Waiting for client connection...");
            Stream.WaitForConnection();
            Logger.Msg("Client connected!");

            Task.Run(() => ReceiveLoop());
            Task.Run(() => ClientLoop());
        }

        private void ClientLoop()
        {
            while (true)
            {

            }
        }

        private async Task ReceiveLoop()
        {
            byte[] buffer = new byte[SilkConstants.PACKET_BUFFER];

            while (Stream!.IsConnected)
            {
                try
                {
                    int read = await Stream.ReadAsync(buffer, 0, buffer.Length);
                    if (read > 0)
                    {
                        byte[] data = new byte[read - 4];
                        Array.Copy(buffer, 4, data, 0, read - 4);
                        HandlePacket(data);
                    }
                }
                catch (IOException e)
                {
                    Logger.Warn($"NamedPipeServer receive loop ended: {e.Message}");
                    break;
                }
                catch (Exception ex)
                {
                    Logger.Error($"NamedPipeServer receive loop fatal: {ex}");
                    break;
                }
            }
        }



        public override void Disconnect()
        {
            Stream?.Dispose();
        }

        public override void Initialize()
        {

        }
        public override void Send(Packet packet)
        {
            if (Stream == null || !Stream.IsConnected)
            {
                Logger.Warn("Stream was null or not connected.");
                return;
            }

            byte[]? data = PacketProtocol.PackPacket(packet);
            if (data == null)
                return;

            Logger.Msg("presend"); 
            try
            {
                Stream.Write(data, 0, data.Length);
            } catch(Exception e)
            {
                Logger.Warn($"NamedPipeServer send error: {e.Message} {e.GetType().Name}");
                return;
            }
            Logger.Msg("sent");
            Stream.Flush();
            Logger.Msg("flushed");
        }
    }
}
