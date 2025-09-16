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

        private NamedPipeClientStream? _clientStream;

        public override void ConnectImpl(string host, int? port)
        {
            _clientStream = new NamedPipeClientStream(".", host, PipeDirection.InOut, PipeOptions.Asynchronous);
            Logger.Msg("Connecting to NamedPipeServer...");
            _clientStream.Connect();
            Logger.Msg("Connected to server!");

            Task.Run(() => ReceiveLoop());

            Logger.Msg("Sending Handshake...");
            Send(new HandshakePacket(NetworkUtils.LocalClient?.ClientID.ToString() ?? Guid.NewGuid().ToString(), "C2SID123"));
            Logger.Msg("Sent.");
        }
        private void ReceiveLoop()
        {
            try
            {
                byte[] buffer = new byte[SilkConstants.PACKET_BUFFER];
                while (_clientStream!.IsConnected)
                {
                    int read = _clientStream.Read(buffer, 0, buffer.Length);
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
            _clientStream?.Dispose();
            _clientStream = null;
        }

        public override void Initialize()
        {
            // Optional initialization logic
        }

        public override void Send(Packet packet)
        {
            if (_clientStream == null || !_clientStream.IsConnected)
            {
                Logger.Warn("Stream was null or not connected.");
                return;
            }

            byte[]? data = PacketProtocol.PackPacket(packet);
            if (data == null)
                return;

            _clientStream.Write(data, 0, data.Length);
            _clientStream.Flush();
        }

    }
}
