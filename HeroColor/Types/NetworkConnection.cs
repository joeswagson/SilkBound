using SilkBound.Network.Packets;
using SilkBound.Packets;
using SilkBound.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SilkBound.Types
{
    public abstract class NetworkConnection
    {
        public PacketHandler PacketHandler { get; private set; }
        public NetworkConnection(PacketHandler packetHandler)
        {
            PacketHandler = packetHandler;
        }
        public Action<byte[]> DataRecieved = new Action<byte[]>((data) =>
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
        public (string?, Packet?) HandlePacket(byte[] data)
        {
            (string?, Packet?) returned = PacketProtocol.UnpackPacket(data);
            PacketHandler.Handle(returned.Item2);
            return returned;
        }
        public abstract void Initialize();
    }
}
