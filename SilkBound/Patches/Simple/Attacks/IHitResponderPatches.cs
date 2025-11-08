using HarmonyLib;
using SilkBound.Extensions;
using SilkBound.Managers;
using SilkBound.Network.Packets.Impl.Sync.Attacks;
using SilkBound.Types.Language;
using SilkBound.Utils;
using System;
using UnityEngine;
namespace SilkBound.Patches.Simple.Attacks
{
    public struct HitFlagData(bool isPacketThread, HealthManager healthManager) {
        public bool IsPacketThread = isPacketThread;
        public string? Path = healthManager.transform.GetPath();
    }
    public class IHitResponderPatches
    {
        public static bool HitPrefix(IHitResponder __instance, [HarmonyArgument(0)] HitInstance hitInstance, ref IHitResponder.HitResponse __result)
        {
            if (!NetworkUtils.Connected) return true;
            bool packetThread = NetworkUtils.IsPacketThread();

            string goPath = (__instance as Component)!.transform.GetPath();
            Type responderType = __instance.GetType();

            #region Non-Packet thread handling
            if (!packetThread)
            {
                NetworkUtils.SendPacketAsync(new SyncHitPacket(hitInstance, goPath)).Void();

                if (responderType == typeof(Breakable))
                {
                    Logger.Msg("Attemping Breakable Sync:", SceneStateManager.ProposeChanges(SceneStateManager.GetCurrent(), StateChange.Method(nameof(SceneState.RegisterBrokenObject), goPath)));
                }
                else if (responderType == typeof(HealthManager))
                {
                    //hitInstance.CanWeakHit = true;
                    var hm = (HealthManager)__instance;
                    Logger.Msg("overwriting healthmanager.msg");

                    //Logger.Msg("Attemping HealthManager Sync:", SceneStateManager.ProposeChanges(SceneStateManager.GetCurrent(), StateChange.Method(nameof(SceneState.RegisterBrokenObject), goPath)));

                    // overwrite of IHitResponder.HitResponse HealthManager.Hit(HitInstance hitInstance)
                    if (hm.isDead)
                    {
                        __result = IHitResponder.Response.None;
                        return false;
                    }
                    if (hm.evasionByHitRemaining > 0f)
                    {
                        __result = IHitResponder.Response.None;
                        return false;
                    }
                    if (hitInstance.HitEffectsType != EnemyHitEffectsProfile.EffectsTypes.LagHit && hitInstance.DamageDealt <= 0 && !hitInstance.CanWeakHit)
                    {
                        __result = IHitResponder.Response.None;
                        return false;
                    }
                    FSMUtility.SendEventToGameObject(hitInstance.Source, "DEALT DAMAGE", false);
                    int cardinalDirection = DirectionUtils.GetCardinalDirection(hitInstance.GetActualDirection(hm.transform, HitInstance.TargetType.Regular));
                    if (hm.IsBlockingByDirection(cardinalDirection, hitInstance.AttackType, hitInstance.SpecialType))
                    {
                        hm.Invincible(hitInstance);
                        __result = IHitResponder.Response.Invincible;
                        return false;
                    }
                    using (new StackFlag<HitFlagData>(new HitFlagData(false, hm)))
                    {
                        hm.TakeDamage(hitInstance);
                    }
                    __result = IHitResponder.Response.DamageEnemy;

                    return false;
                    // end section
                }

                return true;
            }
            #endregion

            #region Packet thread handling

            if (responderType == typeof(HealthManager))
            {
                //hitInstance.CanWeakHit = true;
                var hm = (HealthManager)__instance;
                Logger.Msg("overwriting healthmanager.msg");

                //Logger.Msg("Attemping HealthManager Sync:", SceneStateManager.ProposeChanges(SceneStateManager.GetCurrent(), StateChange.Method(nameof(SceneState.RegisterBrokenObject), goPath)));

                // overwrite of IHitResponder.HitResponse HealthManager.Hit(HitInstance hitInstance)
                if (hm.isDead)
                {
                    __result = IHitResponder.Response.None;
                    return false;
                }
                if (hm.evasionByHitRemaining > 0f)
                {
                    __result = IHitResponder.Response.None;
                    return false;
                }
                if (hitInstance.HitEffectsType != EnemyHitEffectsProfile.EffectsTypes.LagHit && hitInstance.DamageDealt <= 0 && !hitInstance.CanWeakHit)
                {
                    __result = IHitResponder.Response.None;
                    return false;
                }
                FSMUtility.SendEventToGameObject(hitInstance.Source, "DEALT DAMAGE", false);
                int cardinalDirection = DirectionUtils.GetCardinalDirection(hitInstance.GetActualDirection(hm.transform, HitInstance.TargetType.Regular));
                if (hm.IsBlockingByDirection(cardinalDirection, hitInstance.AttackType, hitInstance.SpecialType))
                {
                    hm.Invincible(hitInstance);
                    __result = IHitResponder.Response.Invincible;
                    return false;
                }
                //hitInstance.DamageDealt = 0;
                using (new StackFlag<HitFlagData>(new HitFlagData(true, hm)))
                {
                    //Logger.Msg("firing takedamage");
                    hm.TakeDamage(hitInstance);
                }
                __result = IHitResponder.Response.DamageEnemy;

                return false;
                // end section
            }
            #endregion
            return true;
        }
    }
}
