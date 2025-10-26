using HarmonyLib;
using SilkBound.Managers;
using SilkBound.Utils;
using UnityEngine;

namespace SilkBound.Patches.Simple.Game
{
    [HarmonyPatch(typeof(LogoLanguage))]
    public class LogoLanguagePatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(LogoLanguage.SetSprite))]
        public static bool SetSprite(LogoLanguage __instance)
        {
            if (!SilkConstants.CUSTOM_TITLE)
                return true;

            if(ResourceManager.Resources.CustomTitle.TryGetResource(out Sprite resource) && __instance.spriteRenderer)
            {
                __instance.transform.localScale = new Vector3(
                    2.2347f, 2.2347f, 0.9498f // pulled from game lol
                );

                __instance.spriteRenderer.sprite = resource;

                if (__instance.uiImage)
                    __instance.uiImage.sprite = resource;

                if(__instance.setNativeSize)
                    __instance.uiImage?.SetNativeSize();

                return false;
            }

            return true;
        }
    }
}
