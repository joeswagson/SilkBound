using HarmonyLib;
using SilkBound.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SilkBound.Behaviours;
using SilkBound.Network.Packets.Impl.World;
using SilkBound.Network.Packets;
using SilkBound.Extensions;

namespace SilkBound.Patches.Simple.Attacks
{
    [HarmonyPatch(typeof(ObjectPool))]
    public class ObjectPoolPatches
    {
        internal static string debug_log_target = string.Empty;//"BackdashTriple Effect";

        [HarmonyPostfix]
        [HarmonyPatch(typeof(TestGameObjectActivator), nameof(TestGameObjectActivator.Start))]
        public static void CreateStartupPools(TestGameObjectActivator __instance)
        {
            __instance.gameObject.AddComponentIfNotPresent<SceneStateSyncController>();
            //var scene = SceneManager.GetActiveScene();
            //Logger.Msg("syncing scenestate:", scene.name);
            //SceneStateManager.Fetch(scene.name).Value.Sync(scene);
        }

        //[HarmonyPrefix]
        [HarmonyPostfix]
        [HarmonyPatch(nameof(ObjectPool.Spawn), [typeof(GameObject), typeof(Transform), typeof(Vector3), typeof(Quaternion), typeof(bool)])]
        public static void Spawn(GameObject prefab, Transform parent, Vector3 position, Quaternion rotation, bool stealActiveSpawned = false)
        {
            if (NetworkUtils.LocalClient == null || HeroController.instance == null || NetworkUtils.IsPacketThread())
                return;

            //if (TransactionManager.Fetch<bool>(prefab))
            //{
            //    Logger.Msg("Found key");
            //    TransactionManager.Revoke(prefab);
            //    return;
            //}

            if (SilkConstants.DEBUG && prefab.name == debug_log_target)
                Logger.Stacktrace();

            if (CachedEffects.IsEffect(prefab, out var effect))
            {
                //Logger.Msg($"Intercepted spawn of prefab {prefab.name} at {position}");
                NetworkUtils.SendPacket(new PrefabSpawnPacket(effect.Name, position, rotation, parent?.GetPath(), stealActiveSpawned));
            }

            //return true;
        }
        public static class CachedEffects
        {
            public class CachedEffect(Func<GameObject?> getter, Action<Packet, GameObject>? spawned = null)
            {
                public string Name => Prefab?.name ?? "null";
                public GameObject? _cached = null;
                public GameObject? Prefab
                {
                    get
                    {
                        if (_cached == null || _cached.GetCachedPtr() == IntPtr.Zero)
                            _cached = getter();

                        return _cached;
                    }
                }
                public Action<Packet, GameObject>? Spawned => spawned;
            }

            // terry davis forgive me for this is not what you envisioned
            public static readonly List<CachedEffect> Effects = new()
            {
                { new CachedEffect(() => GlobalSettings.Effects.EnemyNailTerrainThunk)},
                { new CachedEffect(() => HeroController.instance?.nailTerrainImpactEffectPrefab) },
                { new CachedEffect(() => HeroController.instance?.nailTerrainImpactEffectPrefabDownSpike) },
                { new CachedEffect(() => HeroController.instance?.takeHitSingleEffectPrefab) },
                { new CachedEffect(() => HeroController.instance?.takeHitDoubleEffectPrefab) },
                { new CachedEffect(() => HeroController.instance?.takeHitDoubleBlackThreadEffectPrefab) },
                { new CachedEffect(() => HeroController.instance?.takeHitBlackHealthNullifyPrefab) },
                { new CachedEffect(() => HeroController.instance?.takeHitDoubleFlameEffectPrefab) },
                { new CachedEffect(() => HeroController.instance?.softLandingEffectPrefab) },
                { new CachedEffect(() => HeroController.instance?.hardLandingEffectPrefab) },
                { new CachedEffect(() => HeroController.instance?.backDashPrefab.gameObject, (packet, obj)=>{
                    var dash = obj.GetComponent<DashEffect>();
                    Transform root = packet.Sender.Mirror.Root.transform ?? HeroController.instance.transform;
                    dash.transform.localScale = new Vector3(root.localScale.x * -1f, root.localScale.y, root.localScale.z);
                    dash.Play(root.gameObject);
                }) },
                { new CachedEffect(() => HeroController.instance?.jumpEffectPrefab.gameObject, (p,s)=>s.GetComponent<JumpEffects>().Play(p.Sender.Mirror.Root, p.Sender.Mirror.Root.GetComponent<Rigidbody2D>().linearVelocity, Vector3.zero)) },
                { new CachedEffect(() => HeroController.instance?.jumpTrailPrefab) },
                { new CachedEffect(() => HeroController.instance?.fallEffectPrefab) },
                //{ new CachedEffect(() => HeroController.instance?.wallslideDustPrefab) },
                { new CachedEffect(() => HeroController.instance?.artChargeEffect) },
                { new CachedEffect(() => HeroController.instance?.artChargedEffect) },
                { new CachedEffect(() => HeroController.instance?.downspikeBurstPrefab) },
                //{ new CachedEffect(() => HeroController.instance?.dashBurstPrefab) },
                //{ new CachedEffect(() => HeroController.instance?.dashParticles) },
                { new CachedEffect(() => HeroController.instance?.wallPuffPrefab) },
                { new CachedEffect(() => HeroController.instance?.backflipPuffPrefab) },
                { new CachedEffect(() => HeroController.instance?.airDashEffect) },
                { new CachedEffect(() => HeroController.instance?.walldashKickoffEffect) },
                { new CachedEffect(() => HeroController.instance?.umbrellaEffect) },
                { new CachedEffect(() => HeroController.instance?.doubleJumpEffectPrefab) },
                { new CachedEffect(() => HeroController.instance?.canBindEffect) },
                //{ new CachedEffect(() => HeroController.instance?.quickeningEffectPrefab) },
                //{ new CachedEffect(() => HeroController.instance?.quickeningPoisonEffectPrefab) },
                //{ new CachedEffect(() => HeroController.instance?.maggotEffectPrefab) },
                //{ new CachedEffect(() => HeroController.instance?.frostedEffect) },
                { new CachedEffect(() => HeroController.instance?.luckyDiceShieldEffectPrefab) },
                //{ new CachedEffect(() => HeroController.instance?.SlashNoiseMakerFront) },
                //{ new CachedEffect(() => HeroController.instance?.SlashNoiseMakerAbove) },
                //{ new CachedEffect(() => HeroController.instance?.SlashNoiseMakerBelow) },



                // GlobalSettings.Effects
                { new CachedEffect(() => GlobalSettings.Effects.BloodParticlePrefab) },
                { new CachedEffect(() => GlobalSettings.Effects.RageHitEffectPrefab) },
                { new CachedEffect(() => GlobalSettings.Effects.RageHitHealthEffectPrefab) },
                { new CachedEffect(() => GlobalSettings.Effects.WeakHitEffectPrefab) },
                { new CachedEffect(() => GlobalSettings.Effects.EnemyWitchPoisonHitEffectPrefab) },
                { new CachedEffect(() => GlobalSettings.Effects.EnemyWitchPoisonHurtEffectPrefab) },
                { new CachedEffect(() => GlobalSettings.Effects.ReapHitEffectPrefab) },
                { new CachedEffect(() => GlobalSettings.Effects.SpikeSlashEffectPrefab) },
                { new CachedEffect(() => GlobalSettings.Effects.NailClashParryEffect) },
                { new CachedEffect(() => GlobalSettings.Effects.NailClashParryEffectSmall) },
                { new CachedEffect(() => GlobalSettings.Effects.EnemyNailTerrainThunk) },
                { new CachedEffect(() => GlobalSettings.Effects.TinkEffectDullPrefab) },
                { new CachedEffect(() => GlobalSettings.Effects.SilkPossesionObjSing) },
                { new CachedEffect(() => GlobalSettings.Effects.SilkPossesionObjSingNoPuppet) },
                { new CachedEffect(() => GlobalSettings.Effects.SilkPossesionObjSingEnd) },
                { new CachedEffect(() => GlobalSettings.Effects.LifebloodEffectPrefab) },
                { new CachedEffect(() => GlobalSettings.Effects.LifebloodHealEffect) },
                { new CachedEffect(() => GlobalSettings.Effects.EnemyPhysicalPusher) },
                { new CachedEffect(() => GlobalSettings.Effects.BlackThreadEnemyStartEffect) },
                { new CachedEffect(() => GlobalSettings.Effects.BlackThreadEnemyEffect) },
                { new CachedEffect(() => GlobalSettings.Effects.BlackThreadEnemyDeathEffect) },
                { new CachedEffect(() => GlobalSettings.Effects.BlackThreadPooledEffect) }
            };

            public static bool IsEffect(Object go, out CachedEffect effect)
            {
                effect = Effects.FirstOrDefault(e => e.Name == go.name);
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

