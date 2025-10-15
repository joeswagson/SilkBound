using GlobalSettings;
using HarmonyLib;
using SilkBound.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;
using System.Text;
using UnityEngine;
using Logger = SilkBound.Utils.Logger;
using SilkBound.Behaviours;
using static MelonLoader.MelonLogger;
using SilkBound.Network.Packets.Impl.Sync.World;
using SilkBound.Network.Packets.Impl;
using SilkBound.Network.Packets.Impl.World;
using Object = UnityEngine.Object;
using SilkBound.Managers;

namespace SilkBound.Patches.Simple.Attacks
{
    [HarmonyPatch(typeof(ObjectPool))]
    public class ObjectPoolPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(ObjectPool.Spawn), new[] { typeof(GameObject), typeof(Transform), typeof(Vector3), typeof(Quaternion), typeof(bool) })]
        public static bool Spawn(GameObject prefab, Transform parent, Vector3 position, Quaternion rotation, bool stealActiveSpawned = false)
        {
            if (NetworkUtils.LocalClient == null || HeroController.instance == null || NetworkUtils.IsPacketThread())
                return true;

            if (TransactionManager.Fetch<bool>(prefab))
            {
                //Logger.Msg("Found key");
                TransactionManager.Revoke(prefab);
                return true;
            }

            if (CachedEffects.IsEffect(prefab, out var effect))
            {
                Logger.Msg($"Intercepted spawn of prefab {prefab.name} at {position}");
                NetworkUtils.SendPacket(new PrefabSpawnPacket(effect.Name, position, rotation, parent, stealActiveSpawned));
            }

            return true;
        }
        public static class CachedEffects
        {
            public class CachedEffect(Func<Object?> getter)
            {
                public string Name => Prefab?.name ?? "null";
                public Object? _cached = null;
                public Object? Prefab
                {
                    get
                    {
                        return _cached ??= getter();
                    }
                }
            }

            // terry davis forgive me for this is not what you envisioned
            public static readonly List<CachedEffect> Effects = new List<CachedEffect>()
            {
                //{ new CachedEffect(() => GlobalSettings.Effects.EnemyNailTerrainThunk)},

                //{ new CachedEffect(() => HeroController.instance?.nailTerrainImpactEffectPrefab) },
                //{ new CachedEffect(() => HeroController.instance?.nailTerrainImpactEffectPrefabDownSpike) },
                //{ new CachedEffect(() => HeroController.instance?.downspikeEffectPrefabSpawnPoint) },
                //{ new CachedEffect(() => HeroController.instance?.takeHitSingleEffectPrefab) },
                //{ new CachedEffect(() => HeroController.instance?.takeHitDoubleEffectPrefab) },
                //{ new CachedEffect(() => HeroController.instance?.takeHitDoubleBlackThreadEffectPrefab) },
                //{ new CachedEffect(() => HeroController.instance?.takeHitBlackHealthNullifyPrefab) },
                //{ new CachedEffect(() => HeroController.instance?.takeHitDoubleFlameEffectPrefab) },
                //{ new CachedEffect(() => HeroController.instance?.softLandingEffectPrefab) },
                //{ new CachedEffect(() => HeroController.instance?.hardLandingEffectPrefab) },
                //{ new CachedEffect(() => HeroController.instance?.runEffectPrefab) },
                //{ new CachedEffect(() => HeroController.instance?.backDashPrefab) },
                //{ new CachedEffect(() => HeroController.instance?.jumpEffectPrefab) },
                //{ new CachedEffect(() => HeroController.instance?.jumpTrailPrefab) },
                //{ new CachedEffect(() => HeroController.instance?.fallEffectPrefab) },
                //{ new CachedEffect(() => HeroController.instance?.wallslideDustPrefab) },
                //{ new CachedEffect(() => HeroController.instance?.artChargeEffect) },
                //{ new CachedEffect(() => HeroController.instance?.artChargedEffect) },
                //{ new CachedEffect(() => HeroController.instance?.artChargedEffectAnim) },
                //{ new CachedEffect(() => HeroController.instance?.downspikeBurstPrefab) },
                //{ new CachedEffect(() => HeroController.instance?.dashBurstPrefab) },
                //{ new CachedEffect(() => HeroController.instance?.dashParticles) },
                //{ new CachedEffect(() => HeroController.instance?.wallPuffPrefab) },
                //{ new CachedEffect(() => HeroController.instance?.backflipPuffPrefab) },
                //{ new CachedEffect(() => HeroController.instance?.airDashEffect) },
                //{ new CachedEffect(() => HeroController.instance?.walldashKickoffEffect) },
                //{ new CachedEffect(() => HeroController.instance?.umbrellaEffect) },
                //{ new CachedEffect(() => HeroController.instance?.doubleJumpEffectPrefab) },
                //{ new CachedEffect(() => HeroController.instance?.canBindEffect) },
                //{ new CachedEffect(() => HeroController.instance?.quickeningEffectPrefab) },
                //{ new CachedEffect(() => HeroController.instance?.quickeningPoisonEffectPrefab) },
                //{ new CachedEffect(() => HeroController.instance?.maggotEffectPrefab) },
                //{ new CachedEffect(() => HeroController.instance?.frostedEffect) },
                ////{ new CachedEffect(() => HeroController.instance?.SlashNoiseMakerFront) },
                ////{ new CachedEffect(() => HeroController.instance?.SlashNoiseMakerAbove) },
                ////{ new CachedEffect(() => HeroController.instance?.SlashNoiseMakerBelow) },
                //{ new CachedEffect(() => HeroController.instance?.luckyDiceShieldEffectPrefab) }
            };

            public static bool IsEffect(Object go, out CachedEffect effect)
            {
                effect = Effects.FirstOrDefault(e => e.Prefab == go);
                return effect != null;
            }

            public static bool GetEffect(string prefabName, out CachedEffect effect)
            {
                effect = Effects.FirstOrDefault(e => e.Name == prefabName);
                return effect != null;
            }
        }
    }
}