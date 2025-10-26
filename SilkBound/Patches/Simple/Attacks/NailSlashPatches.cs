using HarmonyLib;
using SilkBound.Behaviours;
using SilkBound.Network.Packets.Impl.Sync.Attacks;
using SilkBound.Utils;

namespace SilkBound.Patches.Simple.Attacks
{
    [HarmonyPatch(typeof(NailSlash))]
    public class NailSlashPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(NailSlash.StartSlash))]
        public static bool StartSlash_Prefix(NailSlash __instance)
        {
            if (!NetworkUtils.Connected || NetworkUtils.IsPacketThread())
                return true;

            if (__instance.gameObject.GetComponentInParent<HornetMirror>() != null)
                return true;

            NetworkUtils.SendPacket(new NailSlashPacket(__instance.transform.parent.name, __instance.gameObject.name));

            return true;
        }
    }
}
