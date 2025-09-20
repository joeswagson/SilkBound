using SilkBound.Addons.Events.Abstract;
using SilkBound.Network.Packets;
using SilkBound.Types;

namespace SilkBound.Addons.Events;

public class S2CPacketReceivedEvent(Packet packet, NetworkConnection connection) : SilkboundEvent
{
    public Packet GetPacket => packet;
    public NetworkConnection GetConnection => connection;
}