using SilkBound.Managers;
using SilkBound.Network.Packets;
using SilkBound.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace SilkBound.Network
{
    public class Weaver
    {
        public Weaver(string name, Guid? ClientID = null)
        {
            ClientName = name;
            ClientID = ClientID ?? Guid.NewGuid();
        }

        public Guid ClientID;
        public string ClientName;
        public Skin AppliedSkin = SkinManager.Library["blue"];
    }
}
