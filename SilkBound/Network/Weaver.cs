using SilkBound.Behaviours;
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
        public Weaver(string name, NetworkConnection? connection=null, Guid? ClientID = null)
        {
            ClientName = name;
            Connection = connection;
            this.ClientID = ClientID ?? Guid.NewGuid();
        }

        public Guid ClientID;
        public string ClientName;
        public Skin AppliedSkin = SkinManager.Default;

        public NetworkConnection? Connection;
        public SaveGameData? SaveGame;
        public HornetMirror? Mirror;
    }
}
