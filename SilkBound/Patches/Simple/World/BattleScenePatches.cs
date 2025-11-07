using HarmonyLib;
using HutongGames.PlayMaker;
using SilkBound.Behaviours;
using SilkBound.Extensions;
using SilkBound.Types.Language;
using SilkBound.Utils;
using UnityEngine;

namespace SilkBound.Network.Packets.Impl.Sync.World {
    [HarmonyPatch(typeof(BattleScene))]
    public class BattleScenePatches {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(BattleScene.Start))]
        public static void Start(BattleScene __instance)
        {
            if (!NetworkUtils.Connected || NetworkUtils.IsPacketThread()) return;

            foreach (Transform obj in __instance.gates.transform)
                if (obj.GetComponent<PlayMakerFSM>() is var fsm && fsm != null)
                    NetworkPropagatedGateSensor.AddComponent(__instance, fsm);
        }
        [HarmonyPrefix]
        [HarmonyPatch(nameof(BattleScene.StartBattle))]
        public static bool StartBattle(BattleScene __instance)
        {
            if (StackFlag<BattleScene>.Raised || !NetworkUtils.Connected || NetworkUtils.IsPacketThread()) return true;
            using (new StackFlag<BattleScene>())
            {
                NetworkUtils.SendPacket(new StartBattlePacket(__instance.transform.GetPath())); // ask KINDLY dickhead
                return NetworkUtils.IsServer; // unless....
            }
        }
    }
}
