using SilkBound.Addons.Events.Abstract;
using SilkBound.Network;
using SilkBound.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace SilkBound.Addons.Events
{
    public class WeaverConnectedEvent(Weaver client) : SilkboundEvent
    {
        public Weaver Weaver => client;
    }
}
