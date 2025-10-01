using HarmonyLib;
using SilkBound.Behaviours;
using SilkBound.Network.Packets.Impl.Sync.Attacks;
using SilkBound.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace SilkBound.Patches.Simple.Attacks
{
    [HarmonyPatch(typeof(NailSlash))]
    public class NailSlashPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(NailSlash.StartSlash))]
        public static bool StartSlash_Prefix(NailSlash __instance)
        {
            if(NetworkUtils.LocalConnection == null)
                return true;

            if (__instance.gameObject.GetComponentInParent<HornetMirror>() != null)
                return true;

            NetworkUtils.SendPacket(new NailSlashPacket(NetworkUtils.ClientID, __instance));

            return true;
        }
    }
}
