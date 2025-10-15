using GlobalEnums;
using HarmonyLib;
using SilkBound.Behaviours;
using SilkBound.Managers;
using SilkBound.Network.Packets.Impl.Sync.Attacks;
using SilkBound.Types.Mirrors;
using SilkBound.Utils;
using UnityEngine;
using Logger = SilkBound.Utils.Logger;

namespace SilkBound.Patches.Simple.Hero
{
    [HarmonyPatch(typeof(HeroController))]
    public class HeroControllerPatches
    {
        //[HarmonyPostfix]
        //[HarmonyPatch(typeof(HeroController), nameof(HeroController.EnterScene))]
        //static IEnumerator EnterScene_Postfix(
        //    IEnumerator __result,
        //    TransitionPoint enterGate,
        //    float delayBeforeEnter,
        //    bool forceCustomFade,
        //    Action onEnd,
        //    bool enterSkip)
        //{
            
            
        //    Logger.Debug($"EnterScene start: gate={enterGate.gameObject.transform.GetPath()}, delay={delayBeforeEnter}, forceFade={forceCustomFade}, skip={enterSkip}");

        //    while (__result.MoveNext())
        //        yield return __result.Current;

        //    Logger.Debug("EnterScene end");
        //}

        [HarmonyPostfix]
        [HarmonyPatch("Awake")]
        public static void Spawned(HeroController __instance)
        {
            if (NetworkUtils.LocalClient == null || __instance is HeroControllerMirror) return;

            Logger.Debug("hero spawned: " + __instance.name);

            NetworkUtils.LocalClient.Mirror = HornetMirror.CreateLocal();
            NetworkUtils.LocalClient.ChangeSkin(NetworkUtils.LocalClient.AppliedSkin);
        }

        //[HarmonyPrefix]
        //[HarmonyPatch(nameof(HeroController.HandleCollisionTouching))]
        //public static bool HandleCollisionTouching_Prefix(HeroController __instance, Collision2D collision)
        //{
        //    if (NetworkUtils.LocalClient == null || __instance.GetComponent<HornetMirror>()) return true;

        //    if (__instance.cState.downSpiking && __instance.FindCollisionDirection(collision) == CollisionSide.bottom)
        //        NetworkUtils.SendPacket(new DownspikeVFXPacket(collision, __instance.downspikeEffectPrefabSpawnPoint?.position));

        //    return true;
        //}
    }
}
