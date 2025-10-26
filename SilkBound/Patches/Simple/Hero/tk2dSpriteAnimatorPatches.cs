using HarmonyLib;
using HutongGames.PlayMaker;
using SilkBound.Behaviours;
using SilkBound.Network.Packets.Impl.Mirror;
using SilkBound.Types.Language;
using SilkBound.Utils;
namespace SilkBound.Patches.Simple.Hero
{
    [HarmonyPatch(typeof(tk2dSpriteAnimator))]
    public class tk2dSpriteAnimatorPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch("Play", [typeof(tk2dSpriteAnimationClip), typeof(float), typeof(float)])]
        public static bool Play_Prefix(tk2dSpriteAnimator __instance, tk2dSpriteAnimationClip clip, float clipStartTime, float overrideFps)
        {
            if (!NetworkUtils.Connected || NetworkUtils.IsPacketThread()) return true;

            if (__instance == HeroController.instance?.animCtrl?.animator)
                NetworkUtils.SendPacket(new PlayClipPacket(string.Empty, clip.name, clipStartTime, overrideFps));
            else if (__instance.gameObject.GetComponent<EntityMirror>() is var mirror
                    && mirror != null
                    && mirror.IsLocalOwned
                    && !StackFlag<None>.Raised)
                NetworkUtils.SendPacket(new PlayClipPacket(mirror.NetworkId, clip.name, clipStartTime, overrideFps));

            return true;
        }
    }
}
