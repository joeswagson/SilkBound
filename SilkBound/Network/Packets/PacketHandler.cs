using SilkBound.Packets;
using SilkBound.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace SilkBound.Network.Packets
{
    public abstract class PacketHandler
    {
        public PacketHandler()
        {

        }

        public readonly Dictionary<string, List<Action<Packet>>> Handlers = new Dictionary<string, List<Action<Packet>>>(); // PacketName: [PacketNameHandler1, PacketNameHandler2]

        public void Subscribe(string packetName, Action<Packet> handler)
        {
            if (!Handlers.ContainsKey(packetName))
            {
                Handlers.Add(packetName, new List<Action<Packet>>());
            }

            Handlers[packetName].Add(handler);
        }
        public void Handle(Packet? packet)
        {
            if (packet == null) return;

            if (Handlers.TryGetValue(packet.PacketName, out List<Action<Packet>> handlers))
                foreach (var handler in handlers)
                    handler(packet);
        }
        public abstract void Initialize();
    }
}
