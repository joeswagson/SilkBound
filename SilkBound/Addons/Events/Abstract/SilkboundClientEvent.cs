using SilkBound.Network;
using SilkBound.Utils;

namespace SilkBound.Addons.Events.Abstract;

public abstract class SilkboundClientEvent : SilkboundEvent
{
    public Weaver? GetSender() => NetworkUtils.LocalClient;
}