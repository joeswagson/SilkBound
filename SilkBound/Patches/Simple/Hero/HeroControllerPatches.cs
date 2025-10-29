using GlobalSettings;
using HarmonyLib;
using SilkBound.Behaviours;
using SilkBound.Network.Packets.Impl.Sync.Attacks;
using SilkBound.Network.Packets.Impl.Sync.Mirror;
using SilkBound.Types;
using SilkBound.Types.Mirrors;
using SilkBound.Utils;
using UnityEngine;
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
            #if !SERVER
            NetworkUtils.LocalClient.ChangeSkin(NetworkUtils.LocalClient.AppliedSkin);
            #endif
            if (SilkConstants.DEBUG && SilkConstants.GETALLPOWERUPS) GameManager.instance.GetAllPowerups();
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(HeroController.StartDashEffect))]
        public static void DashVFX(HeroController __instance)
        {
            if (!NetworkUtils.Connected || NetworkUtils.IsPacketThread())
                return;

            float num = 1f
                + (__instance.IsUsingQuickening ? 0.25f : 0f)
                + (Gameplay.SprintmasterTool.IsEquipped ? 0.25f : 0f)
                + (!Mathf.Approximately(__instance.sprintSpeedAddFloat.Value, 0f) ? 0.25f : 0f);


            NetworkUtils.SendPacket(new AirDashVFXPacket(__instance.cState.onGround, !__instance.cState.wallSliding && !__instance.cState.onGround, __instance.cState.wallSliding, __instance.dashingDown, num));
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(HeroController.Die))]
        public static bool Die_Prefix(HeroController __instance)
        {
            if (!NetworkUtils.Connected || NetworkUtils.IsPacketThread())
                return true;

            if (Server.CurrentServer.Settings.GhostAfterDeath)
            {
                NetworkUtils.LocalClient.Mirror.Ghost();
                NetworkUtils.SendPacket(new TransitionGhostPacket(true));
                return false;
            }

            return true;
        }

        /// experimental
        //[HarmonyPrefix]
        //[HarmonyPatch(nameof(HeroController.instance), MethodType.Getter)]
        //public static bool get_instance(ref HeroController __result)
        //{
        //    if (StackFlag<PacketHandlerContext>.Raised && !StackFlag<HornetMirror>.Raised)
        //    {
        //        //Logger.Msg("HeroController.instance flags:", NetworkUtils.IsPacketThread(), StackFlag<PacketHandlerContext>.Raised, !StackFlag<HornetMirror>.Raised);
        //        if (StackFlag<PacketHandlerContext>.Value.Sender.Mirror?.MirrorController is var mirrorController && mirrorController != null)
        //            __result = mirrorController;

        //        return false;
        //    }

        //    return true;
        //}

        static bool first = true;
        [HarmonyPrefix]
        [HarmonyPatch(nameof(HeroController.HeroRespawned))]
        public static bool Respawn_Prefix(HeroController __instance) {
            if (!SilkConstants.DEBUG || !NetworkUtils.Connected)
                return true;

            if (first)
                HeroController.instance.transform.position.Set(76.8323f, 17.5686f, 0.004f);

            first = false;

            return true;
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
