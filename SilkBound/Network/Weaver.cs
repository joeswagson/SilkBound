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
        public Weaver(NetworkConnection networkConnection)
        {
            ClientID = Guid.NewGuid();
            Connection = networkConnection;
        }

        public Guid ClientID;
        public string ClientName;
        public Skin AppliedSkin = SkinManager.Library["blue"];
        public NetworkConnection Connection;
        
        public void Disconnect()
        {
            Connection.Disconnect();
        }
    }
}
