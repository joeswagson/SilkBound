using SilkBound.Network.Packets;
using System.Collections.Generic;

namespace SilkBound.Types {
    public abstract class NetworkServer(PacketHandler packetHandler) : NetworkConnection(packetHandler) {
        public abstract void SendIncluding(Packet packet, IEnumerable<NetworkConnection> include);
        public abstract void SendExcluding(Packet packet, IEnumerable<NetworkConnection> exclude);
        public void SendExcept(Packet packet, NetworkConnection exclude)
        {
            SendExcluding(packet, [exclude]);
        }
    }
}
