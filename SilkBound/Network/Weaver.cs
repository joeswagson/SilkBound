using SilkBound.Behaviours;
using SilkBound.Managers;
using SilkBound.Network.Packets.Impl.Mirror;
using SilkBound.Types;
using SilkBound.Utils;
using System;
namespace SilkBound.Network
{
    public class Weaver(string name, NetworkConnection? connection = null, Guid? clientID = null) {

        //public AuthToken Token;
        public Guid ClientID = clientID ?? Guid.NewGuid();
        public string ClientName = name;
        public string? AppliedSkinName;
        public Skin AppliedSkin => SkinManager.GetOrDefault(AppliedSkinName ?? string.Empty);
        public bool IsLocal => NetworkUtils.ClientID == ClientID;

        // SHUT UP SHUT UP SHUT UP SHUT UP SHUT UP SHUT UP SHUT UP SHUT UP SHUT UP SHUT UP SHUT UP SHUT UP SHUT UP SHUT UP SHUT UP SHUT UP GET OUT OF MY HEAD GAAAAGH
        public NetworkConnection Connection = connection!;
        public MultiplayerSaveGameData SaveGame = null!;
        public HornetMirror Mirror = null!;

        public void ChangeSkin(Skin skin)
        {
            if (IsLocal)
                NetworkUtils.SendPacket(new SkinUpdatePacket(skin.SkinName));
            
            var collection = IsLocal ? HeroController.instance?.animCtrl?.animator?.Sprite?.collection : Mirror?.MirrorSpriteCollection;
            if (collection != null)
                SkinManager.ApplySkin(collection, skin);
            AppliedSkinName = skin.SkinName;
        }
        public Weaver[] GetPlayersInScene() => Server.CurrentServer.GetPlayersInScene();
        public int GetPlayerCountInScene() => Server.CurrentServer.GetPlayerCountInScene();

        public bool InScene(string? sceneName) => Mirror?.Scene == sceneName;
    }
}