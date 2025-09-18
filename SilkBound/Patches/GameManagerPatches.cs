using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace SilkBound.Patches
{
    [HarmonyPatch(typeof(GameManager))]
    public class GameManagerPatches
    {
        [HarmonyPrefix]
        [Harmony]
    }
}
