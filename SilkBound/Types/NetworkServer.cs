using SilkBound.Network.Packets;
using SilkBound.Network.Packets.Handlers;
using SilkBound.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading.Tasks;

namespace SilkBound.Types
{
    public abstract class NetworkServer : NetworkConnection
    {
        public NetworkServer() : base(new ShallowPacketHandler())
        {
        }
        public NetworkServer(PacketHandler packetHandler) : base(packetHandler)
        {
        }
    }
}
