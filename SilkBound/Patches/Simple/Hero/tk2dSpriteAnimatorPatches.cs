using HarmonyLib;
using SilkBound.Extensions;
using SilkBound.Network.Packets.Impl;
using SilkBound.Network.Packets.Impl.Mirror;
using SilkBound.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace SilkBound.Patches.Simple.Hero
{
    [HarmonyPatch(typeof(tk2dSpriteAnimator))]
    public class tk2dSpriteAnimatorPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch("Play", new Type[] { typeof(tk2dSpriteAnimationClip), typeof(float), typeof(float) })]
        public static bool Play_Prefix(tk2dSpriteAnimator __instance, tk2dSpriteAnimationClip clip, float clipStartTime, float overrideFps)
        {
            //Logger.Msg(NetworkUtils.IsPacketThread(), __instance.gameObject.name);
            if (NetworkUtils.LocalClient != null && __instance == HeroController.instance?.GetComponent<tk2dSpriteAnimator>())
                NetworkUtils.SendPacket(new PlayClipPacket(NetworkUtils.LocalClient.ClientID, clip.name, clipStartTime, overrideFps));
            return true;
        }
    }
}
