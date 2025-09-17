using SilkBound.Network.Packets;
using SilkBound.Packets;
using SilkBound.Packets.Impl;
using SilkBound.Utils;
using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading.Tasks;

namespace SilkBound.Types
{
    public class NamedPipeConnection : NetworkConnection
    {
        public NamedPipeConnection(string host) : base(new ClientPacketHandler())
        {
            Connect(host, null);
        }

        public NamedPipeClientStream? Stream;

        public override void ConnectImpl(string host, int? port)
        {
            Stream = new NamedPipeClientStream(".", host, PipeDirection.InOut, PipeOptions.Asynchronous);
            Logger.Msg("Connecting to NamedPipeServer...");
            Stream.Connect();
            Logger.Msg("Connected to server!");

            Task.Run(() => ReceiveLoop());

            //Logger.Msg("Sending Handshake...");
            //Send(new HandshakePacket(NetworkUtils.LocalClient?.ClientID.ToString() ?? Guid.NewGuid().ToString(), "C2SID123"));
            //Logger.Msg("Sent.");
        }
        private void ReceiveLoop()
        {
            try
            {
                byte[] buffer = new byte[SilkConstants.PACKET_BUFFER];
                while (Stream!.IsConnected)
                {
                    int read = Stream.Read(buffer, 0, buffer.Length);
                    if (read > 0)
                    {
                        byte[] data = new byte[read];
                        Array.Copy(buffer, data, read);
                        HandlePacket(data);
                    }
                }
            }
            catch (IOException e)
            {
                Logger.Warn($"NamedPipeConnection receive loop ended: {e.Message}");
            }
        }


        public override void Disconnect()
        {
            Stream?.Dispose();
            Stream = null;
        }

        public override void Initialize()
        {
            // Optional initialization logic // TODO: remove chatgpt comment because i made it write the recieve loop for me (i HATE named pipes <3 )
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
