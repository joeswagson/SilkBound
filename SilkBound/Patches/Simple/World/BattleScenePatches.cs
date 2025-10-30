using HarmonyLib;
using SilkBound.Behaviours;
using SilkBound.Extensions;
using SilkBound.Utils;
using UnityEngine;

namespace SilkBound.Network.Packets.Impl.Sync.World
{
    [HarmonyPatch(typeof(BattleScene))]
    public class BattleScenePatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(BattleScene.Start))]
        public static void Start(BattleScene __instance)
        {
            Logger.Stacktrace();
            if (!NetworkUtils.Connected || NetworkUtils.IsPacketThread()) return;

            foreach (Transform obj in __instance.gates.transform)
                if (obj.GetComponent<PlayMakerFSM>() is var fsm && fsm != null)
                    NetworkPropagatedGateSensor.AddComponent(__instance, fsm);
        }
        [HarmonyPostfix]
        [HarmonyPatch(nameof(BattleScene.StartBattle))]
        public static void StartBattle(BattleScene __instance)
        {
            if (!NetworkUtils.Connected || NetworkUtils.IsPacketThread()) return;

            NetworkUtils.SendPacket(new StartBattlePacket(__instance.transform.GetPath()));
        }
    }
}
