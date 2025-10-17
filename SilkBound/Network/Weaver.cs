using GlobalEnums;
using MelonLoader;
using SilkBound.Behaviours;
using SilkBound.Managers;
using SilkBound.Network.Packets;
using SilkBound.Network.Packets.Impl.Mirror;
using SilkBound.Network.Packets.Impl.Sync.World;
using SilkBound.Types;
using SilkBound.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Logger = SilkBound.Utils.Logger;

namespace SilkBound.Network
{
    public class Weaver
    {
        public Weaver(string name, NetworkConnection? connection=null, Guid? clientID = null)
        {
            ClientName = name;
            Connection = connection;
            ClientID = clientID ?? Guid.NewGuid();
            //Token = token;
        }

        //public AuthToken Token;
        public Guid ClientID;
        public string ClientName;
        public Skin AppliedSkin = SkinManager.Default;
        public bool IsLocal => NetworkUtils.ClientID == ClientID;

        // SHUT UP SHUT UP SHUT UP SHUT UP SHUT UP SHUT UP SHUT UP SHUT UP SHUT UP SHUT UP SHUT UP SHUT UP SHUT UP SHUT UP SHUT UP SHUT UP GET OUT OF MY HEAD GAAAAGH
        public NetworkConnection Connection = null!;
        public MultiplayerSaveGameData SaveGame = null!;
        public HornetMirror Mirror = null!;

        public void ChangeSkin(Skin skin)
        {
            if (IsLocal)
                NetworkUtils.SendPacket(new SkinUpdatePacket(skin.SkinName));

            var collection = IsLocal ? HeroController.instance?.animCtrl?.animator?.Sprite?.collection : Mirror?.MirrorSpriteCollection;
            if (collection != null)
                SkinManager.ApplySkin(collection, skin);

            AppliedSkin = skin;
        }
    }
}