using HarmonyLib;
using SilkBound.Network.Packets.Impl.Sync.Attacks;
using SilkBound.Utils;

namespace SilkBound.Patches.Simple.Attacks
{
    [HarmonyPatch(typeof(Downspike))]
    public class DownspikePatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(Downspike.StartSlash))]
        public static bool StartSlash_Prefix(Downspike __instance)
        {
            if (!NetworkUtils.Connected || NetworkUtils.IsPacketThread()) 
                return true;

            NetworkUtils.SendPacket(new DownspikePacket(__instance.transform.parent.name, __instance.gameObject.name, false));
            return true;
        }
        [HarmonyPrefix]
        [HarmonyPatch(nameof(Downspike.CancelAttack))]
        public static bool CancelAttack_Prefix(Downspike __instance)
        {
            if (!NetworkUtils.Connected || NetworkUtils.IsPacketThread())
                return true;

            NetworkUtils.SendPacket(new DownspikePacket(__instance.transform.parent.name, __instance.gameObject.name, true));
            return true;
        }
    }
}
