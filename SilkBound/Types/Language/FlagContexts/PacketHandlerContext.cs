using SilkBound.Network;
using SilkBound.Network.Packets;

namespace SilkBound.Types.Language.FlagContexts
{
    public struct PacketHandlerContext
    {
        public Weaver Sender;
        public Packet Packet;
        public NetworkConnection Connection;
    }
}
