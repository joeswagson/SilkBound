using HarmonyLib;
using SilkBound.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SilkBound.Patches.Simple.World {
    [HarmonyPatch(typeof(Time))]
    public class TimePatches {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(Time.timeScale), MethodType.Setter)]
        public static bool Prefix(ref float value)
        {
            if (!NetworkUtils.Connected) return true;

            value = 1f;
            return false;
        }
    }
}
