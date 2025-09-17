using SilkBound.Types;

namespace SilkBound.Addons.Events.Abstract;

public class SilkboundServerEvent : SilkboundEvent
{
    public Server? GetServer() => Server.CurrentServer;
}