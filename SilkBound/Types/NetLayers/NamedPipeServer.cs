using SilkBound.Network.Packets;
using SilkBound.Packets;
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

        public override void ConnectImpl(string host, int? port)
        {
            Stream = new NamedPipeServerStream(host, PipeDirection.InOut);
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

        private void ReceiveLoop()
        {
            byte[] buffer = new byte[SilkConstants. PACKET_BUFFER];

            while (Stream!.IsConnected)
            {
                try
                {
                    int read = Stream.Read(buffer, 0, buffer.Length);
                    if (read > 0)
                    {
                        byte[] data = new byte[read];
                        Array.Copy(buffer, data, read);
                        HandlePacket(data);
                    }
                }
                catch (IOException e)
                {
                    Logger.Warn($"NamedPipeServer receive loop ended: {e.Message}");
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

            Stream.Write(data, 0, data.Length);
            Stream.Flush();
        }

    }
}
