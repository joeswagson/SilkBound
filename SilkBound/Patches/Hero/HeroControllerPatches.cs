using HarmonyLib;
using HutongGames.PlayMaker.Actions;
using MelonLoader;
using SharpDX;
using SilkBound.Behaviours;
using SilkBound.Extensions;
using SilkBound.Managers;
using SilkBound.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Device;
using Logger = SilkBound.Utils.Logger;

namespace SilkBound.Patches.Hero
{
    [HarmonyPatch(typeof(HeroController))]
    public class HeroControllerPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(HeroController), nameof(HeroController.EnterScene))]
        static IEnumerator EnterScene_Postfix(
            IEnumerator __result,
            TransitionPoint enterGate,
            float delayBeforeEnter,
            bool forceCustomFade,
            Action onEnd,
            bool enterSkip)
        {
            
            
            Logger.Debug($"EnterScene start: gate={enterGate.gameObject.transform.GetPath()}, delay={delayBeforeEnter}, forceFade={forceCustomFade}, skip={enterSkip}");

            while (__result.MoveNext())
                yield return __result.Current;

            Logger.Debug("EnterScene end");
        }


        //public static readonly Color capeShade = new Color(118, 45, 86);
        //public static readonly Color capeShade2 = new Color(80, 31, 59);
        public static readonly Color CAPE_PRIMARY = new Color(118f / 255f, 45f / 255f, 86f / 255f);

        public static float ColorDistance(Color src, Color dst)
        {
            return (Math.Abs(src.r - dst.r) * Math.Abs(src.g - dst.g) + Math.Abs(src.b - dst.b)) / 3;
        }
        public static float RedDistance(Color src, Color dst)
        {
            return Math.Abs(src.r * 255f - dst.r * 255f);
        }

        public static void HsvToRgb(double h, double s, double v, out int r, out int g, out int b)
        {
            h = h % 360;
            if (h < 0) h += 360;

            double c = v * s;
            double x = c * (1 - Math.Abs(h / 60 % 2 - 1));
            double m = v - c;

            double rPrime = 0, gPrime = 0, bPrime = 0;

            if (h < 60) { rPrime = c; gPrime = x; }
            else if (h < 120) { rPrime = x; gPrime = c; }
            else if (h < 180) { gPrime = c; bPrime = x; }
            else if (h < 240) { gPrime = x; bPrime = c; }
            else if (h < 300) { rPrime = x; bPrime = c; }
            else { rPrime = c; bPrime = x; }

            r = (int)((rPrime + m) * 255); 
            g = (int)((gPrime + m) * 255);
            b = (int)((bPrime + m) * 255);
        }

        [HarmonyPostfix]
        [HarmonyPatch("Awake")]
        public static void Spawned(HeroController __instance)
        {
            if (NetworkUtils.LocalClient == null) return;
            Melon<ModMain>.Logger.Msg("hero spawned: " + __instance.name);

            HornetMirror mirror = __instance.gameObject.AddComponent<HornetMirror>();
            TickManager.OnTick += ()=> { mirror.HeroSyncTick(__instance); };

            MeshRenderer charRender = __instance.GetComponent<MeshRenderer>();
            if (!charRender) return;

            var collection = __instance.GetComponent<tk2dSpriteAnimator>().Sprite.Collection.textures;

            for (int i = 0; i < collection.Length; i++)
            {
                string key = $"atlas{i}";

                if (!NetworkUtils.LocalClient.AppliedSkin.Textures.TryGetValue(key, out Texture2D skinTex))
                    continue;

                Texture atlas = collection[i];
                Texture2D readableAtlas = new Texture2D(atlas.width, atlas.height, TextureFormat.RGBA32, false);

                RenderTexture rt = RenderTexture.GetTemporary(atlas.width, atlas.height, 0);
                Graphics.Blit(atlas, rt);
                RenderTexture.active = rt;
                readableAtlas.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                RenderTexture.active = null;
                RenderTexture.ReleaseTemporary(rt);

                __instance.GetComponent<tk2dSpriteAnimator>().Sprite.Collection.materialInsts[i].SetTexture("_MainTex", skinTex);
                __instance.GetComponent<tk2dSpriteAnimator>().Sprite.Collection.materials[i].SetTexture("_MainTex", skinTex);
                collection[i] = skinTex;
            }

            MelonLogger.Msg("Skin applied from NetworkUtils.LocalClient.Skin.");
        }
    }
}
