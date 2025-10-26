using HarmonyLib;
using SilkBound.Behaviours;
using SilkBound.Utils;

namespace SilkBound.Patches.Simple.Hero
{
    [HarmonyPatch(typeof(HeroNailImbuement))]
    public class HeroNailImbuementPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(HeroNailImbuement.SetElement))]
        public static bool SetElement_Prefix(HeroNailImbuement __instance, NailElements element)
        {
            if (!NetworkUtils.Connected || HornetMirror.IsMirror(__instance.gameObject, out _)) return true;

            return false;
        }
    }
}
