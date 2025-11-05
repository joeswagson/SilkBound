using SilkBound.Extensions;
using SilkBound.Network.Packets;
using SilkBound.Utils;
using System;
using System.Threading.Tasks;

namespace SilkBound.Types
{
    public abstract class NetworkConnection(PacketHandler packetHandler, string? host=null, int? port=null) {
        public PacketHandler PacketHandler { get; private set; } = packetHandler;
        public abstract bool IsConnected { get; }

        public Action<byte[]> DataRecieved = new((data) =>
        {
            
        });

        public string? Host { get; private set; } = host;
        public int? Port { get; private set; } = port;

        public async Task Connect(string host, int? port)
        {
            Host = host;
            Port = port;

            await ConnectImpl(Host, Port);
        }

        public abstract Task ConnectImpl(string host, int? port);
        public abstract void Dispose();

        public void Disconnect(string reason="Unspecified.")
        {
            NetworkUtils.Disconnect(reason);
        }

        /// <summary>
        /// Inform the network layer to schedule a packet send.
        /// </summary>
        /// <param name="packetData">The serialized packet data. Acquired through <see cref="PacketProtocol.PackPacket(Packet)"/> or an equivalent extension.</param>
        public abstract void Send(byte[] packetData);

        /// <summary>
        /// Pack and send a packet over the network. Note that using this method on seperate connections does not prevent reserializing the packet. To avoid reserialization, preprocess the packet with <see cref="PacketProtocol.PackPacket(Packet)"/> or an extension and call <see cref="Send(byte[]?)"/> directly.
        /// </summary>
        /// <param name="packet">The packet instance to send.</param>
        public void Send(Packet packet)
        {
            if(packet.TryPack(out var packetData))
                Send(packetData);
        }


        public (string?, Guid?, Packet?) HandlePacket(byte[] data)
        {
            (string?, Guid?, Packet?) returned = PacketProtocol.UnpackPacket(data);
            PacketHandler.Handle(returned.Item3, this);
            return returned;
        }
        public abstract void Initialize();
    }
}
