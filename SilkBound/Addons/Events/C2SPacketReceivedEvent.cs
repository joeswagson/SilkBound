using SilkBound.Addons.Events.Abstract;
using SilkBound.Network.Packets;
using SilkBound.Types;

namespace SilkBound.Addons.Events;

public class C2SPacketReceivedEvent(Packet packet, NetworkConnection connection, bool cancel=false) : SilkboundEvent
{
    public Packet Packet => packet;
    public NetworkConnection Connection => connection;
    public bool Cancelled => cancel;
}