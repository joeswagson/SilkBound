using SilkBound.Addons.Events.Abstract;
using SilkBound.Network;

namespace SilkBound.Addons.Events
{
    public class WeaverConnectedEvent(Weaver client) : SilkboundEvent
    {
        public Weaver Weaver => client;
    }
}
