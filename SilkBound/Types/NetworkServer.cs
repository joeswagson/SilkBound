using SilkBound.Network.Packets;
using System.Collections.Generic;

namespace SilkBound.Types
{
    public abstract class NetworkServer(PacketHandler packetHandler) : NetworkConnection(packetHandler)
    {
        public abstract void SendIncluding(Packet packet, List<NetworkConnection> include);
        public abstract void SendExcluding(Packet packet, List<NetworkConnection> exclude);
        public void SendExcept(Packet packet, NetworkConnection exclude)
        {
            SendExcluding(packet, [exclude]);
        }
    }
}
