using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SilkBound.Patches.Simple.Attacks
{
    [HarmonyPatch(typeof(ObjectPoolExtensions))]
    public class ObjectPoolExtensionsPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(ObjectPoolExtensions.Spawn), new[] { typeof(GameObject), typeof(Transform), typeof(Vector3), typeof(Quaternion), typeof(bool)})] // public static GameObject Spawn(GameObject prefab, Transform parent, Vector3 position, Quaternion rotation, bool stealActiveSpawned = false)
        public static bool Spawn(GameObject prefab, Transform parent, Vector3 position, Quaternion rotation, ref GameObject __result, bool stealActiveSpawned = false)
        {
            return true;
        }
    }
}
