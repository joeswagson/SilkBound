using HarmonyLib;
using HutongGames.PlayMaker.Actions;
using SilkBound.Managers;
using SilkBound.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace SilkBound.Patches.FSM
{
    [HarmonyPatch(typeof(ExitFromTransitionGate))]
    public class ExitFromTransitionGatePatches
    {
        [HarmonyPrefix]
        [HarmonyPatch("OnEnter")]
        public static bool OnEnter(ExitFromTransitionGate __instance)
        {
            Logger.Debug("ExitFromTransitionGate OnEnter");
            if (!NetworkUtils.IsConnected) return true;

            bool? isFromNetwork = TransactionManager.Fetch<bool?>(__instance);
            if (isFromNetwork != null)
            {
                Logger.Debug("Transaction found to transition scenes. Allowing.");
                return true;
            }
            else
            {
                Logger.Debug("No transaction found to transition scenes. Blocking and asking server.");
                //NetworkUtils.LocalConnection!.Send(new )
                return false;
            }
        }
    }
}
