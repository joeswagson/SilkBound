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
        public NetworkServer(PacketHandler packetHandler) : base(packetHandler)
        {
        }
        public abstract void SendIncluding(Packet packet, List<NetworkConnection> include);
        public abstract void SendExcluding(Packet packet, List<NetworkConnection> exclude);
        public void SendExcept(Packet packet, NetworkConnection exclude)
        {
            SendExcluding(packet, new List<NetworkConnection>() { exclude });
        }
    }
}
