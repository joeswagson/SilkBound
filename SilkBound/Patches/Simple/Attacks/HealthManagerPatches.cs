using GlobalEnums;
using GlobalSettings;
using HarmonyLib;
using SilkBound.Behaviours;
using SilkBound.Patches.Simple.Game;
using SilkBound.Utils;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SilkBound.Patches.Simple.Attacks {
    [HarmonyPatch(typeof(HealthManager))]
    public class HealthManagerPatches {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(HealthManager.Start))]
        public static void Start(HealthManager __instance)
        {
            if (!NetworkUtils.Connected) return;

            //if (__instance.gameObject.layer == LayerMask.NameToLayer("Enemies") && __instance.isActiveAndEnabled && __instance.gameObject.scene == SceneManager.GetActiveScene())
            if(FsmLogPatches.IsHandled(__instance.gameObject.LocateMyFSM("Control")?.Fsm))
            {
                Tracker.Track(__instance.gameObject);
                EntityMirror.Create(__instance.gameObject);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(HealthManager.TakeDamage))]
        public static bool TakeDamage_Prefix(HealthManager __instance, HitInstance hitInstance)
        {
            if (!NetworkUtils.Connected) return true;
            if (NetworkUtils.LocalClient?.Mirror?.IsGhost ?? false)
                return false;

            if (hitInstance.HitEffectsType == EnemyHitEffectsProfile.EffectsTypes.Full && __instance.hasBlackThreadState && __instance.blackThreadState.IsVisiblyThreaded)
            {
                hitInstance.HitEffectsType = EnemyHitEffectsProfile.EffectsTypes.Minimal;
            }
            if (__instance.IsImmuneTo(hitInstance, true))
            {
                return false;
            }
            DamageEnemies component = hitInstance.Source.GetComponent<DamageEnemies>();
            if (component && component.AwardJournalKill)
            {
                __instance.WillAwardJournalKill = true;
            }
            hitInstance = __instance.ApplyDamageScaling(hitInstance);
            if ((hitInstance.SpecialType & SpecialTypes.RapidBullet) != SpecialTypes.None)
            {
                __instance.rapidBulletTimer = 0.15f;
                __instance.rapidBulletCount++;
                if (__instance.rapidBulletCount > 1)
                {
                    hitInstance.DamageDealt /= __instance.rapidBulletCount;
                    if (hitInstance.DamageDealt < 1)
                    {
                        hitInstance.DamageDealt = 1;
                    }
                }
            }
            if ((hitInstance.SpecialType & SpecialTypes.RapidBomb) != SpecialTypes.None)
            {
                __instance.rapidBombTimer = 0.75f;
                __instance.rapidBombCount++;
                if (__instance.rapidBombCount > 1)
                {
                    Debug.Log(string.Concat(
                    [
                    "Rapid Bomb trying to deal ",
                    hitInstance.DamageDealt.ToString(),
                    " damage. Getting divided by ",
                    __instance.rapidBombCount.ToString(),
                    "..."
                    ]));
                    hitInstance.DamageDealt /= __instance.rapidBombCount;
                    if (hitInstance.DamageDealt < 1)
                    {
                        hitInstance.DamageDealt = 1;
                    }
                    Debug.Log("New rapid bomb damage: " + hitInstance.DamageDealt.ToString());
                }
            }
            if (hitInstance.AttackType == AttackTypes.ExtractMoss && __instance.isMossExtractable && QuestManager.GetQuest("Extractor Green").IsAccepted && !QuestManager.GetQuest("Extractor Green").IsCompleted)
            {
                FSMUtility.SendEventToGameObject(__instance.gameObject, "EXTRACT", false);
                EventRegister.SendEvent(EventRegisterEvents.StartExtractM, null);
                return false;
            }
            if (hitInstance.AttackType == AttackTypes.ExtractMoss && __instance.isSwampExtractable && QuestManager.GetQuest("Extractor Swamp").IsAccepted && !QuestManager.GetQuest("Extractor Swamp").IsCompleted)
            {
                FSMUtility.SendEventToGameObject(__instance.gameObject, "EXTRACT", false);
                EventRegister.SendEvent(EventRegisterEvents.StartExtractSwamp, null);
                return false;
            }
            if (hitInstance.AttackType == AttackTypes.ExtractMoss && __instance.isBluebloodExtractable)
            {
                FSMUtility.SendEventToGameObject(__instance.gameObject, "EXTRACT", false);
                EventRegister.SendEvent(EventRegisterEvents.StartExtractBlueblood, null);
                return false;
            }
            if (hitInstance.CriticalHit)
            {
                hitInstance.DamageDealt = Mathf.RoundToInt((float) hitInstance.DamageDealt * Gameplay.WandererCritMultiplier);
                hitInstance.MagnitudeMultiplier *= Gameplay.WandererCritMagnitudeMult;
                GameObject wandererCritEffect = Gameplay.WandererCritEffect;
                if (wandererCritEffect)
                {
                    wandererCritEffect.Spawn(__instance.transform.TransformPoint(__instance.effectOrigin)).transform.SetRotation2D(hitInstance.GetOverriddenDirection(__instance.transform, HitInstance.TargetType.Regular));
                }
                GameManager.instance.FreezeMoment(FreezeMomentTypes.HeroDamageShort, null);
            }
            int cardinalDirection = DirectionUtils.GetCardinalDirection(hitInstance.GetActualDirection(__instance.transform, HitInstance.TargetType.Regular));
            if (hitInstance.HunterCombo)
            {
                GameObject hunterComboDamageEffect = Gameplay.HunterComboDamageEffect;
                if (hunterComboDamageEffect)
                {
                    GameObject gameObject = hunterComboDamageEffect.Spawn(__instance.transform.TransformPoint(__instance.effectOrigin));
                    switch (cardinalDirection)
                    {
                        case 0:
                            gameObject.transform.SetRotation2D(0f);
                            break;
                        case 1:
                        case 3:
                            gameObject.transform.SetRotation2D(90f);
                            break;
                        case 2:
                            gameObject.transform.SetRotation2D(180f);
                            break;
                    }
                    Vector3 localScale = hunterComboDamageEffect.transform.localScale;
                    if (Gameplay.HunterCrest3.IsEquipped && HeroController.instance.HunterUpgState.IsComboMeterAboveExtra)
                    {
                        Vector2 hunterCombo2DamageExtraScale = Gameplay.HunterCombo2DamageExtraScale;
                        localScale.x *= hunterCombo2DamageExtraScale.x;
                        localScale.y *= hunterCombo2DamageExtraScale.y;
                    }
                    gameObject.transform.localScale = localScale;
                }
            }
            __instance.lastHitInstance = hitInstance;
            __instance.directionOfLastAttack = cardinalDirection;
            __instance.lastAttackType = hitInstance.AttackType;
            bool flag = hitInstance.DamageDealt <= 0 && hitInstance.HitEffectsType != EnemyHitEffectsProfile.EffectsTypes.LagHit;
            if (hitInstance.AttackType == AttackTypes.Heavy)
            {
                FSMUtility.SendEventToGameObject(__instance.gameObject, "TOOK HEAVY DAMAGE", false);
            }
            FSMUtility.SendEventToGameObject(__instance.gameObject, "HIT", false);
            NonBouncer component2 = __instance.GetComponent<NonBouncer>();
            bool flag2 = false;
            if (component2)
            {
                flag2 = component2.active;
            }
            if (!flag2)
            {
                FSMUtility.SendEventToGameObject(hitInstance.Source, "HIT LANDED", false);
                FSMUtility.SendEventToGameObject(hitInstance.Source, "DEALT ACTUAL DAMAGE", false);
            }
            FSMUtility.SendEventToGameObject(__instance.gameObject, "TOOK DAMAGE", false);
            if (!__instance.hasBlackThreadState || !__instance.blackThreadState.IsInForcedSing)
            {
                FSMUtility.SendEventToGameObject(__instance.gameObject, "SING DURATION END", false);
            }
            if (__instance.sendHitTo != null)
            {
                FSMUtility.SendEventToGameObject(__instance.sendHitTo, "HIT", false);
            }
            if (flag)
            {
                FSMUtility.SendEventToGameObject(__instance.gameObject, "WEAK HIT", false);
            }
            if (hitInstance.AttackType == AttackTypes.Spell)
            {
                FSMUtility.SendEventToGameObject(__instance.gameObject, "TOOK SPELL DAMAGE", false);
            }
            if (hitInstance.AttackType == AttackTypes.Explosion)
            {
                FSMUtility.SendEventToGameObject(__instance.gameObject, "TOOK EXPLOSION DAMAGE", false);
            }
            if (__instance.recoil != null)
            {
                __instance.recoil.RecoilByDirection(cardinalDirection, hitInstance.MagnitudeMultiplier);
            }
            bool flag3 = false;
            bool flag4 = false;
            AttackTypes attackType = hitInstance.AttackType;
            switch (attackType)
            {
                case AttackTypes.Nail:
                    break;
                case AttackTypes.Generic:
                    flag4 = true;
                    goto IL_8A0;
                case AttackTypes.Spell:
                    goto IL_565;
                default:
                    if (attackType != AttackTypes.NailBeam)
                    {
                        if (attackType != AttackTypes.Heavy)
                        {
                            goto IL_8A0;
                        }
                        if (!hitInstance.Source || !DamageEnemies.IsNailAttackObject(hitInstance.Source))
                        {
                            goto IL_565;
                        }
                    }
                    break;
            }
            HeroController.instance.NailHitEnemy(__instance, hitInstance);
        IL_565:
            if (hitInstance.AttackType == AttackTypes.Nail && __instance.enemyType != HealthManager.EnemyTypes.Shade && __instance.enemyType != HealthManager.EnemyTypes.Armoured && !__instance.DoNotGiveSilk)
            {
                if (!flag)
                {
                    HeroController.instance.SilkGain(hitInstance);
                }
                if (hitInstance.SilkGeneration != HitSilkGeneration.None && PlayerData.instance.CurrentCrestID == "Reaper" && HeroController.instance.ReaperState.IsInReaperMode)
                {
                    float angleMin = 0f;
                    float angleMax = 360f;
                    int i;
                    switch (__instance.reaperBundles)
                    {
                        case HealthManager.ReaperBundleTiers.Normal:
                            i = HeroController.instance.GetReaperPayout();
                            break;
                        case HealthManager.ReaperBundleTiers.Reduced:
                            i = 1;
                            break;
                        case HealthManager.ReaperBundleTiers.None:
                            i = 0;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    int num = i;
                    if (num > 0)
                    {
                        float degrees;
                        if (__instance.flingSilkOrbsAimObject != null)
                        {
                            Vector3 position = __instance.flingSilkOrbsAimObject.transform.position;
                            Vector3 position2 = __instance.transform.position;
                            float y = position.y - position2.y;
                            float x = position.x - position2.x;
                            float num2 = Mathf.Atan2(y, x) * 57.295776f;
                            angleMin = num2 - 45f;
                            angleMax = num2 + 45f;
                            degrees = num2;
                        } else if (__instance.flingSilkOrbsDown)
                        {
                            angleMin = 225f;
                            angleMax = 315f;
                            degrees = 270f;
                        } else
                        {
                            switch (cardinalDirection)
                            {
                                case 0:
                                    angleMin = 315f;
                                    angleMax = 415f;
                                    degrees = 0f;
                                    break;
                                case 1:
                                    angleMin = 45f;
                                    angleMax = 135f;
                                    degrees = 90f;
                                    break;
                                case 2:
                                    angleMin = 125f;
                                    angleMax = 225f;
                                    degrees = 180f;
                                    break;
                                case 3:
                                    angleMin = 225f;
                                    angleMax = 315f;
                                    degrees = 270f;
                                    break;
                                default:
                                    degrees = 0f;
                                    break;
                            }
                        }
                        FlingUtils.SpawnAndFling(new FlingUtils.Config {
                            Prefab = Gameplay.ReaperBundlePrefab,
                            AmountMin = num,
                            AmountMax = num,
                            SpeedMin = 25f,
                            SpeedMax = 50f,
                            AngleMin = angleMin,
                            AngleMax = angleMax
                        }, __instance.transform, __instance.effectOrigin, null, -1f);
                        GameObject reapHitEffectPrefab = Effects.ReapHitEffectPrefab;
                        if (reapHitEffectPrefab)
                        {
                            Vector2 original;
                            float rotation;
                            switch (DirectionUtils.GetCardinalDirection(degrees))
                            {
                                case 1:
                                    original = new Vector2(1f, 1f);
                                    rotation = 90f;
                                    break;
                                case 2:
                                    original = new Vector2(-1f, 1f);
                                    rotation = 0f;
                                    break;
                                case 3:
                                    original = new Vector2(1f, -1f);
                                    rotation = 90f;
                                    break;
                                default:
                                    original = new Vector2(1f, 1f);
                                    rotation = 0f;
                                    break;
                            }
                            GameObject gameObject2 = reapHitEffectPrefab.Spawn(__instance.transform.TransformPoint(__instance.effectOrigin));
                            gameObject2.transform.SetRotation2D(rotation);
                            gameObject2.transform.localScale = original.ToVector3(gameObject2.transform.localScale.z);
                        }
                    }
                }
            }
            if (!flag && hitInstance.HitEffectsType != EnemyHitEffectsProfile.EffectsTypes.LagHit)
            {
                flag3 = true;
            }
            flag4 = true;
        IL_8A0:
            Vector3 position3 = (hitInstance.Source!.transform.position + __instance.transform.position) * 0.5f + __instance.effectOrigin;
            if (flag4 && hitInstance.SlashEffectOverrides != null && hitInstance.SlashEffectOverrides.Length != 0)
            {
                GameObject[] array = hitInstance.SlashEffectOverrides.SpawnAll(__instance.transform.position);
                float num3 = Mathf.Sign(HeroController.instance.transform.localScale.x);
                GameObject[] array2 = array;
                for (int i = 0; i < array2.Length; i++)
                {
                    GameObject gameObject3 = array2[i];
                    if (gameObject3)
                    {
                        Vector3 initialScale = gameObject3.transform.localScale;
                        Vector3 initialScale2 = initialScale;
                        initialScale2.x *= num3;
                        gameObject3.transform.localScale = initialScale2;
                        if (!gameObject3.GetComponent<ActiveRecycler>())
                        {
                            GameObject closureEffect = gameObject3;
                            RecycleResetHandler.Add(gameObject3, delegate () {
                                closureEffect.transform.localScale = initialScale;
                            });
                        }
                    }
                }
                flag3 = false;
            }
            if (flag3 && __instance.slashImpactPrefab)
            {
                GameObject gameObject4 = __instance.slashImpactPrefab.Spawn(position3, Quaternion.identity);
                float num4 = 1.7f;
                switch (cardinalDirection)
                {
                    case 0:
                        gameObject4.transform.SetRotation2D((float) UnityEngine.Random.Range(340, 380));
                        gameObject4.transform.localScale = new Vector3(num4, num4, num4);
                        break;
                    case 1:
                        gameObject4.transform.SetRotation2D((float) UnityEngine.Random.Range(70, 110));
                        gameObject4.transform.localScale = new Vector3(num4, num4, num4);
                        break;
                    case 2:
                        gameObject4.transform.SetRotation2D((float) UnityEngine.Random.Range(340, 380));
                        gameObject4.transform.localScale = new Vector3(-num4, num4, num4);
                        break;
                    case 3:
                        gameObject4.transform.SetRotation2D(180f - hitInstance.GetOverriddenDirection(__instance.transform, HitInstance.TargetType.Regular));
                        gameObject4.transform.localScale = new Vector3(num4, num4, num4);
                        break;
                }
            }
            if (__instance.hitEffectReceiver != null && hitInstance.AttackType != AttackTypes.RuinsWater)
            {
                __instance.hitEffectReceiver.ReceiveHitEffect(hitInstance);
            }
            int num5 = Mathf.RoundToInt((float) hitInstance.DamageDealt * hitInstance.Multiplier);
            if (__instance.damageOverride)
            {
                num5 = 1;
            }
            CheatManager.NailDamageStates nailDamage = CheatManager.NailDamage;
            if (nailDamage != CheatManager.NailDamageStates.InstaKill)
            {
                if (nailDamage == CheatManager.NailDamageStates.NoDamage)
                {
                    num5 = 0;
                }
            } else
            {
                num5 = int.MaxValue;
            }
            __instance.hasTakenDamage = true;
            if (__instance.sendDamageTo == null)
            {
                SetHP(__instance, Mathf.Max(__instance.hp - num5, -1000));
                //__instance.hp = Mathf.Max(__instance.hp - num5, -1000);
            } else
            {
                SetHP(__instance.sendDamageTo, Mathf.Max(__instance.sendDamageTo.hp - num5, -1000));
                //__instance.sendDamageTo.hp = Mathf.Max(__instance.sendDamageTo.hp - num5, -1000);
            }
            if (hitInstance.NonLethal && __instance.hp <= 0)
            {
                SetHP(__instance, 1);
                //__instance.hp = 1;
            }
            if (hitInstance.PoisonDamageTicks > 0)
            {
                DamageTag poisonPouchDamageTag = Gameplay.PoisonPouchDamageTag;
                __instance.tagDamageTaker.AddDamageTagToStack(poisonPouchDamageTag, hitInstance.PoisonDamageTicks);
            }
            if (hitInstance.ZapDamageTicks > 0)
            {
                DamageTag zapDamageTag = Gameplay.ZapDamageTag;
                __instance.tagDamageTaker.AddDamageTagToStack(zapDamageTag, hitInstance.ZapDamageTicks);
            }
            if (__instance.hp > 0)
            {
                var del = AccessTools.Field(typeof(HealthManager), "TookDamage").GetValue(__instance) as Action;
                del?.Invoke();

                __instance.NonFatalHit(hitInstance.IgnoreInvulnerable);
                __instance.ApplyStunDamage(hitInstance.StunDamage);
                return false;
            }
            float num6 = hitInstance.GetActualDirection(__instance.transform, HitInstance.TargetType.Corpse);
            float magnitudeMultForType = hitInstance.GetMagnitudeMultForType(HitInstance.TargetType.Corpse);
            bool disallowDropFling;
            if (hitInstance.Source != null)
            {
                Transform root = hitInstance.Source.transform.root;
                if (root.CompareTag("Player") && !hitInstance.UseCorpseDirection && num6.IsWithinTolerance(Mathf.Epsilon, 270f))
                {
                    num6 = (float) ((root.lossyScale.x < 0f) ? 0 : 180);
                }
                disallowDropFling = hitInstance.Source.GetComponent<BreakItemsOnContact>();
            } else
            {
                disallowDropFling = false;
            }
            __instance.DieDropFling(hitInstance.ToolDamageFlags, disallowDropFling);
            __instance.Die(new float?(num6), hitInstance.AttackType, hitInstance.NailElement, hitInstance.Source, hitInstance.IgnoreInvulnerable, magnitudeMultForType, false, disallowDropFling);

            return true;
        }

        static void SetHP(HealthManager instance, int hp)
        {
            if (NetworkUtils.IsPacketThread())
                return;

            instance.hp = hp;
        }

        //[HarmonyTranspiler]
        //public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        //{
        //    Logger.Msg("Flag not raised:", !StackFlag<bool>.Raised);
        //    if (!NetworkUtils.IsConnected || !StackFlag<bool>.Raised)
        //    {
        //        foreach (var instr in instructions)
        //            yield return instr;
        //        yield break;
        //    }

        //    var heroControllerType = AccessTools.TypeByName("HeroController");
        //    var heroInstanceField = AccessTools.Field(heroControllerType, "instance");

        //    var hpField = AccessTools.Field(typeof(HealthManager), "hp");

        //    List<CodeInstruction> instructionsList = instructions.ToList();
        //    foreach (var instr in instructions)
        //    {
        //        Logger.Msg(instr.opcode, instr.operand);
        //        if (instr.opcode == OpCodes.Ldsfld && instr.operand.Equals(heroInstanceField))
        //            continue;

        //        if (instr.opcode == OpCodes.Ldfld && instr.operand.Equals(hpField))
        //        {
        //            var nextInstr = instructionsList[instructions.IndexOf(instruction => instruction == instr) - 1];

        //            Logger.Msg("previous:", nextInstr.opcode.ToString());

        //            if (nextInstr.opcode == OpCodes.Ldc_I4 || nextInstr.opcode == OpCodes.Ldc_I4_S)
        //            {
        //                int valueBeingSet = (int)nextInstr.operand;
        //                var data = StackFlag<HitFlagData>.Value;

        //                Logger.Msg($"HealthManager.TakeDamage transpiler detected health set to {valueBeingSet} on {(data.Path ?? "unknown path")}");
        //                if (!data.IsPacketThread)
        //                    NetworkUtils.SendPacket(new UpdateHealthPacket(data.Path ?? string.Empty, valueBeingSet));
        //            }

        //        }

        //        yield return instr;
        //    }
        //}
    }
}
