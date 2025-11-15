using SilkBound.Extensions;
using SilkBound.Network.Packets;
using SilkBound.Utils;
using System;
using System.Threading.Tasks;

namespace SilkBound.Network.NetworkLayers
{
    public abstract class NetworkConnection {
        public NetworkStats Stats;
        public PacketHandler PacketHandler { get; private set; }
        public string? Host { get; private set; }
        public int? Port { get; private set; }
        private readonly bool Local;
        public NetworkConnection(PacketHandler packetHandler, string? host= null, int? port = null, NetworkStats? stats=null)
        {
            PacketHandler = packetHandler;
            Host = host;
            Port = port;
            Stats = stats ?? new NetworkStats(this);
            Local = stats == null;

            Initialize();
        }
        public abstract bool IsConnected { get; }

        public Action<byte[]> DataRecieved = new((data) =>
        {
            
        });


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
            NetworkUtils.Disconnect(this, reason);
        }

        /// <summary>
        /// Inform the network layer to schedule a packet send.
        /// </summary>
        /// <param name="packetData">The serialized packet data. Acquired through <see cref="PacketProtocol.PackPacket(Packet)"/> or an equivalent extension.</param>
        protected abstract Task Write(byte[] packetData);
        public async Task Send(byte[] packetData)
        {
            if(Local) Stats.LogPacketSent(packetData);

            Task task = Write(packetData);
            await task.ConfigureAwait(false);
            if (Local && !task.IsCompletedSuccessfully)
                Stats.LogPacketSentFaulted(packetData);
        }

        /// <summary>
        /// Pack and send a packet over the network. Note that using this method on seperate connections does not prevent reserializing the packet. To avoid reserialization, preprocess the packet with <see cref="PacketProtocol.PackPacket(Packet)"/> or an extension and call <see cref="Send(byte[]?)"/> directly.
        /// </summary>
        /// <param name="packet">The packet instance to send.</param>
        public async Task Send(Packet packet)
        {
            if(packet.TryPack(out var packetData))
                await Send(packetData);
        }


        public (ushort, Guid, Packet?)? HandlePacket(byte[] data)
        {
            Stats.LogBytesRead(data);
            (ushort, Guid, Packet?)? returned = PacketProtocol.UnpackPacket(data);
            Stats.LogPacketRead(data, returned?.Item3);
            PacketHandler.Handle(returned?.Item3, this);
            return returned;
        }
        public abstract void Initialize();
    }
}
