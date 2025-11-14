using HarmonyLib;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using SilkBound.Behaviours;
using SilkBound.Extensions;
using SilkBound.Managers;
using SilkBound.Network.Packets.Impl.Sync.Entity;
using SilkBound.Types.Language;
using SilkBound.Utils;
using System;
using System.Linq;
using UnityEngine;

namespace SilkBound.Patches.Simple.Game {
    [HarmonyPatch(typeof(FsmLog))]
    public class FsmLogPatches {
        public enum Handler {
            NONE,
            ENEMY
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayMakerFSM), nameof(PlayMakerFSM.Awake))]
        public static bool PlayMakerFSM_Awake_Prefix(PlayMakerFSM __instance)
        {
            //if (NetworkUtils.Connected && __instance?.gameObject?.layer == LayerMask.NameToLayer("Enemies")) {
            Tracker.Track(__instance.gameObject);
            if (GetHandler(__instance.Fsm) != Handler.NONE)
            {
                new StackFlagPole<PlayMakerFSM>(__instance);
            }

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(FsmLog.LoggingEnabled), MethodType.Setter)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Compiler generated method name prefixes lowercause \"set_\"")]
        public static bool set_LoggingEnabled(ref bool value)
        {
            if (NetworkUtils.Connected)
                value = true;

            return true;
        }
        public static bool IsHandled(Fsm? fsm)
        {
            return GetHandler(fsm) != Handler.NONE;
        }
        public static Handler GetHandler(Fsm? fsm)
        {
            if (fsm == null || !NetworkUtils.Connected)
                return Handler.NONE;

            //Logger.Msg(
            //    "isMenu:", !GameManager.instance.IsMenuScene(),
            //    "active:", (fsm.FsmComponent?.isActiveAndEnabled ?? false),
            //    "layer:", fsm.GameObject != null ? LayerMask.LayerToName(fsm.GameObject.layer) : "null gameobj",
            //    "isenemy:", fsm.GameObject != null ? fsm.GameObject?.layer == LayerMask.NameToLayer("Enemies") : "null gameobj");
            if ((!GameManager.instance?.IsMenuScene() ?? false)
                && (fsm.FsmComponent?.isActiveAndEnabled ?? false)
                && fsm.GameObject?.layer == LayerMask.NameToLayer("Enemies"))
                return Handler.ENEMY;

            return Handler.NONE;
        }

        #region Entity FSM Patches
        //i did ALL of this for NOTHING. kill me bro <3
        //private static void WriteVariable(BinaryWriter writer, NamedVariable variable)
        //{
        //    writer.Write(variable.useVariable);
        //    writer.Write(variable.name);
        //}
        //private static T ReadVariable<T>(BinaryReader reader) where T : NamedVariable
        //{
        //    bool useVariable = reader.ReadBoolean();
        //    string name = reader.ReadString();

        //    switch (typeof(T))
        //    {
        //        case Type type when type == typeof(FsmBool):
        //            return (new FsmBool()
        //            {
        //                value = reader.ReadBoolean(),

        //                useVariable = useVariable,
        //                name = name,
        //                showInInspector = true,
        //                tooltip = "",
        //                networkSync = false
        //            } as T)!;
        //        case Type type when type == typeof(FsmString):
        //            return (new FsmString()
        //            {
        //                value = reader.ReadString(),

        //                useVariable = useVariable,
        //                name = name,
        //                showInInspector = true,
        //                tooltip = "",
        //                networkSync = false
        //            } as T)!;
        //        default:
        //            throw new Exception("ReadVariable did not pass a valid FsmEventTarget variable type.");
        //    }
        //}
        //public static FsmEventTarget DeserializeTarget(byte[] data)
        //{
        //    using (var ms = new MemoryStream())
        //    using (var reader = new BinaryReader(ms))
        //    {
        //        FsmEventTarget.EventTarget target = (FsmEventTarget.EventTarget)reader.ReadInt32();
        //        FsmBool excludeSelf = ReadVariable<FsmBool>(reader);
        //        OwnerDefaultOption ownerOption = (OwnerDefaultOption)reader.ReadInt32();
        //        GameObject? obj = UnityObjectExtensions.FindObjectFromFullName(reader.ReadString());
        //        FsmOwnerDefault? gameObject = null;
        //        if (obj != null)
        //            gameObject = new FsmOwnerDefault() { ownerOption = ownerOption, gameObject = obj };
        //        FsmString fsmName = ReadVariable<FsmString>(reader);
        //        FsmBool sendToChildren = ReadVariable<FsmBool>(reader);
        //        PlayMakerFSM? fsmComponent = UnityObjectExtensions.FindComponent<PlayMakerFSM>(reader.ReadString());

        //        return new FsmEventTarget()
        //        {
        //            target = target,
        //            excludeSelf = excludeSelf,
        //            gameObject = gameObject,
        //            fsmName = fsmName,
        //            sendToChildren = sendToChildren,
        //            fsmComponent = fsmComponent
        //        };
        //    }
        //}
        //public static byte[] SerializeTarget(FsmEventTarget eventTarget)
        //{
        //    using (var ms = new MemoryStream())
        //    using (var writer = new BinaryWriter(ms))
        //    {
        //        writer.Write((int)eventTarget.target);
        //        WriteVariable(writer, eventTarget.excludeSelf);
        //        writer.Write((int)eventTarget.gameObject.ownerOption);
        //        writer.Write(((GameObject)eventTarget.gameObject.gameObject.obj).transform.GetPath());
        //        WriteVariable(writer, eventTarget.fsmName);
        //        WriteVariable(writer, eventTarget.sendToChildren);
        //        writer.Write(eventTarget.fsmComponent);

        //        return ms.ToArray();
        //    }
        //}

        [HarmonyPrefix]
        //[HarmonyPatch(nameof(FsmLog.LogEvent))]
        [HarmonyPatch(typeof(Fsm), nameof(Fsm.Event), [typeof(FsmEventTarget), typeof(FsmEvent)])]
        public static bool LogEvent(Fsm __instance, FsmEventTarget eventTarget, FsmEvent fsmEvent)
        {
            if (!NetworkUtils.Connected || NetworkUtils.IsPacketThread()) goto GAME;

            switch (GetHandler(__instance))
            {
                case Handler.NONE: goto GAME;

                case Handler.ENEMY:
                {
                    EntityMirror mirror = __instance.FsmComponent.GetComponent<EntityMirror>();

                    if (mirror == null || !mirror.IsLocalOwned)
                        goto GAME;

                    var goPath = __instance.FsmComponent.transform.GetPath();
                    //FsmEvent.
                    //Logger.Msg("FSM len:", ChunkedTransfer.Pack(__instance).Sum(x=>x.Length));
                    //Logger.Msg("Firing", $"{__instance.GameObjectName}::{__instance.Name}.{fsmEvent.name}");
                    if (FsmExtensions.Identifier(__instance) == Guid.Empty)
                        Logger.Warn("Sending null fsm id!", __instance.GameObject.transform.GetPath());
                    Logger.Warn("Sending fsm id!", FsmExtensions.Identifier(__instance));
                    __instance.Send(f => new FSMEventPacket(f, fsmEvent.name));
                    //NetworkUtils.SendPacket(new FSMEventPacket(
                    //    goPath,
                    //    __instance.Name,
                    //    fsmEvent.name
                    //));

                    //if (mirror.HealthManager.battleScene == null)
                    //    SceneStateManager.ProposeChanges(SceneStateManager.GetCurrent(), StateChange.Method(nameof(SceneState.RegisterFSMEvent), goPath, __instance.Name, fsmEvent.name));

                    break;
                }
            }

            // oops
            goto GAME;

        #region Control Labels
        GAME:
            return true;
        NONE:
            return false;
            #endregion
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Fsm), nameof(Fsm.EnterState), [typeof(FsmState)])]
        public static bool EnterState(Fsm __instance, FsmState state)
        {
            if (!NetworkUtils.Connected || NetworkUtils.IsPacketThread()) goto GAME;

            switch (GetHandler(__instance))
            {
                case Handler.NONE: goto GAME;
                case Handler.ENEMY:
                    if (!__instance.GetMirror(out EntityMirror? mirror))
                        goto GAME;

                    if (!mirror.IsLocalOwned)
                        goto GAME;

                    __instance.Send(f => new FSMStatePacket(f, FsmStateType.Enter, state.Name));
                    break;
            }

            goto GAME;

        #region Control Labels
        GAME:
            return true;
        NONE:
            return false;
            #endregion
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Fsm), nameof(Fsm.ExitState), [typeof(FsmState)])]
        public static bool ExitState(Fsm __instance, FsmState state)
        {
            if (!NetworkUtils.Connected || NetworkUtils.IsPacketThread()) goto GAME;

            switch (GetHandler(__instance))
            {
                case Handler.NONE: goto GAME;
                case Handler.ENEMY:
                    if (!__instance.GetMirror(out EntityMirror? mirror))
                        goto GAME;

                    if (!mirror.IsLocalOwned)
                        goto GAME;

                    __instance.Send(f => new FSMStatePacket(f, FsmStateType.Exit, state.Name));
                    break;
            }

            goto GAME;

        #region Control Labels
        GAME:
            return true;
        NONE:
            return false;
            #endregion
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(FsmLog.LogStart))]
        public static bool LogStart(FsmLog __instance, FsmState startState)
        {
            if (__instance.Fsm == null) return false;

            switch (GetHandler(__instance.Fsm))
            {
                case Handler.NONE: return false;

                case Handler.ENEMY:
                {
                    EntityMirror mirror = __instance.Fsm.FsmComponent.GetComponent<EntityMirror>();

                    if (mirror == null || !mirror.IsLocalOwned)
                        break;

                    var goPath = __instance.Fsm.FsmComponent.transform.GetPath();
                    //FsmEvent.
                    //Logger.Msg("Firing", $"{__instance.Fsm.GameObjectName}::{__instance.Fsm.Name}.START({startState.Name})");
                    __instance.Fsm.Send(f => new FSMStatusPacket(f, true));
                    //NetworkUtils.SendPacket(new FSMStatusPacket(
                    //    goPath,
                    //    __instance.Fsm.Name,
                    //    true
                    //));

                    //if (mirror.HealthManager.battleScene == null)
                    //    SceneStateManager.ProposeChanges(SceneStateManager.GetCurrent(), StateChange.Method(nameof(SceneState.RegisterFSMStatus), goPath, __instance.Fsm.Name, true));

                    break;
                }
            }

            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(FsmLog.LogStop))]
        public static bool LogStop(FsmLog __instance)
        {
            if (__instance.Fsm == null) return false;

            switch (GetHandler(__instance.Fsm))
            {
                case Handler.NONE: return false;

                case Handler.ENEMY:
                {
                    EntityMirror mirror = __instance.Fsm.FsmComponent.GetComponent<EntityMirror>();

                    if (mirror == null || !mirror.IsLocalOwned)
                        break;

                    var goPath = __instance.Fsm.FsmComponent.transform.GetPath();
                    //FsmEvent.
                    //Logger.Msg("Firing", $"{__instance.Fsm.GameObjectName}::{__instance.Fsm.Name}.STOP");
                    __instance.Fsm.Send(f => new FSMStatusPacket(f, false));
                    //NetworkUtils.SendPacket(new FSMStatusPacket(
                    //    goPath,
                    //    __instance.Fsm.Name,
                    //    false
                    //));

                    //if (mirror.HealthManager.battleScene == null)
                    //    SceneStateManager.ProposeChanges(SceneStateManager.GetCurrent(), StateChange.Method(nameof(SceneState.RegisterFSMStatus), goPath, __instance.Fsm.Name, false));

                    break;
                }
            }

            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(EventRegister), nameof(EventRegister.ReceiveEvent))]
        public static bool EventRegister_ReceiveEvent_Prefix(EventRegister __instance)
        {
            if (!NetworkUtils.Connected) return true;

            if (__instance.GetComponent<EntityMirror>() is var mirror && mirror != null)
                NetworkUtils.SendPacket(new EventRegisterPacket(__instance.transform.GetPath(), __instance.SubscribedEvent));

            return true;
        }


        #region Manual Behaviours

        #region Mossbone Mother
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ObjectJitter), nameof(ObjectJitter.DoTranslate))]
        public static bool DoTranslate(ObjectJitter __instance)
        {
            if (!NetworkUtils.Connected) return true;

            if (__instance.Fsm.FsmComponent.GetComponent<EntityMirror>() is var mirror && mirror != null)
                return mirror.IsLocalOwned;

            return true;
        }
        #endregion

        #region Generic
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Tk2dPlayAnimation), nameof(Tk2dPlayAnimation.DoPlayAnimation))]
        public static bool DoPlayAnimation(Tk2dPlayAnimation __instance)
        {
            if (!NetworkUtils.Connected) return true;

            if (__instance.gameObject?.GameObject?.value?.GetComponent<EntityMirror>() is var mirror && mirror != null)
            {
                Logger.Msg("Calling original:", mirror.IsLocalOwned);
                return mirror.IsLocalOwned;
            }

            return true;
        }
        #endregion

        #endregion
        #endregion

        //[HarmonyPrefix]
        //[HarmonyPatch(nameof(PlayMakerFSM.SendEvent))]
        //public static bool SendEventPrefix(PlayMakerFSM __instance, string eventName)
        //{
        //    if (!NetworkUtils.Connected) return true;

        //    GameObject gameObject = __instance.gameObject;
        //    if (gameObject.GetComponent<EntityMirror>() is var mirror && mirror != null)
        //    {
        //        Logger.Msg("FSM Event Data:", mirror.NetworkId, mirror.IsLocalOwned);
        //        if (!mirror.IsLocalOwned)
        //            return false;

        //        SceneStateManager.ProposeChanges(SceneStateManager.Fetch(gameObject.scene.name).Result, StateChange.Method(nameof(SceneState.RegisterFSMEvent), eventName));
        //        NetworkUtils.SendPacket(new FSMEventPacket(gameObject.transform.GetPath(), eventName));
        //        Logger.Msg("sending event", eventName);
        //    } else if (gameObject.GetComponent<Lever>() is var lever && lever != null)
        //    {
        //        //lever.
        //    }

        //    return true;
        //}
    }
}
