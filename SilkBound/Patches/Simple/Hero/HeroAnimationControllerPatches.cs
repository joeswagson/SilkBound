using HarmonyLib;
using SilkBound.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace SilkBound.Patches.Simple.Hero
{
    [HarmonyPatch(typeof(HeroAnimationController))]
    public class HeroAnimationControllerPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(HeroAnimationController.Play), new Type[] { typeof(string), typeof(float) })]
        public static bool Play_Prefix(HeroAnimationController __instance, string clipName, float speedMultiplier = 1f)
        {
            if (NetworkUtils.LocalClient == null) return true;
            if (__instance != HeroController.instance.GetComponent<HeroAnimationController>()) return true;

            NetworkUtils.SendPacket(new Network.Packets.Impl.PlayClipPacket(NetworkUtils.LocalClient.ClientID, clipName, speedMultiplier));

            return true;
        }
    }
}
