using SilkBound.Network.Packets;
using SilkBound.Utils;
using System;
using System.Threading.Tasks;

namespace SilkBound.Types
{
    public abstract class NetworkConnection(PacketHandler packetHandler) {
        public PacketHandler PacketHandler { get; private set; } = packetHandler;
        public abstract bool IsConnected { get; }

        public Action<byte[]> DataRecieved = new((data) =>
        {
            
        });

        public string? Host { get; private set; }
        public int? Port { get; private set; }

        public void Connect(string host, int? port)
        {
            this.Host = host;
            this.Port = port;

            Task.Run(() => // TODO: ENSURE THAT THIS DOESNT CRASH!!!
            {
                ConnectImpl(Host, Port);
            });
        }

        public abstract void ConnectImpl(string host, int? port);
        public abstract void Disconnect();

        public abstract void Send(Packet packet);


        public (string?, Guid?, Packet?) HandlePacket(byte[] data)
        {
            (string?, Guid?, Packet?) returned = PacketProtocol.UnpackPacket(data);
            PacketHandler.Handle(returned.Item3, this);
            return returned;
        }
        public abstract void Initialize();
    }
}
