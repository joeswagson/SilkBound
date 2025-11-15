using HutongGames.PlayMaker.Actions;
using SilkBound.Network.Packets;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace SilkBound.Network.NetworkLayers {
    public abstract class NetworkServer(PacketHandler packetHandler, string? host = null, int? port = null) : NetworkConnection(packetHandler, host, port) {
        public abstract Task SendIncluding(Packet packet, IEnumerable<NetworkConnection> include);
        public abstract Task SendExcluding(Packet packet, IEnumerable<NetworkConnection> exclude);
        public async Task SendExcept(Packet packet, NetworkConnection exclude)
        {
            await SendExcluding(packet, [exclude]);
        }

        public abstract void HandleDisconnect(NetworkConnection connection);
    }
}
