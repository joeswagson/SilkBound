using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SilkBound.Managers {
    public class SkinBundle(AssetBundle src) {
        public AssetBundle Source = src;
        public string Name => Source.name;
        public Texture2D? Atlas0;
        public Texture2D? Atlas1;
        public Texture2D? Atlas2;
        public Texture2D? Atlas3;

        public Dictionary<int, Texture2D> AtlasLookup = [];

        private static async Task<T> ProcessAssetLoad<T>(AssetBundleRequest request) where T : Object
        {
            await request;
            return (T) request.GetResult();
        }
        private static async Task<T> Load<T>(AssetBundle src, string name) where T : Object => await ProcessAssetLoad<T>(src.LoadAssetAsync<T>(name));
        public static SkinBundle LoadBundle(AssetBundle src)
        {
            var t = LoadBundleAsync(src);
            t.Wait();
            return t.Result;
        }
        public static async Task<SkinBundle> LoadBundleAsync(AssetBundle src)
        {
            var atlas0 = await Load<Texture2D>(src, "atlas0");
            var atlas1 = await Load<Texture2D>(src, "atlas1");
            var atlas2 = await Load<Texture2D>(src, "atlas2");
            var atlas3 = await Load<Texture2D>(src, "atlas3");

            SkinBundle skinBundle = new(src) {
                Atlas0 = atlas0,
                Atlas1 = atlas1,
                Atlas2 = atlas2,
                Atlas3 = atlas3,
                AtlasLookup = new() {
                    { 0, atlas0 },
                    { 1, atlas1 },
                    { 2, atlas2 },
                    { 3, atlas3 }
                }
            };
            return skinBundle;
        }
    }
    public class Skin(Dictionary<string, Texture2D> textures, string skinName = "Unnamed") {
        public Dictionary<string, Texture2D> Textures { get; private set; } = textures;
        public string SkinName { get; private set; } = skinName;

        public static Skin LoadFromFolder(string path)
        {
            var textures = new Dictionary<string, Texture2D>();

            for (int i = 0; i <= 3; i++)
            {
                string filename = $"atlas{i}.png";
                string filePath = Path.Combine(path, filename);

                if (File.Exists(filePath))
                {
                    Texture2D tex = new(2, 2, TextureFormat.RGBA32, false);
                    tex.LoadImage(File.ReadAllBytes(filePath));
                    tex.Apply();
                    textures.Add(Path.GetFileNameWithoutExtension(filename), tex);
                }
            }

            return new Skin(textures, Path.GetFileName(path));
        }

        public static Skin Deserialize(SkinBundle bundle)
        {
            var t = DeserializeAsync(bundle);
            t.Wait();
            return t.Result;
        }

        public static async Task<Skin> DeserializeAsync(SkinBundle bundle)
        {
            var textures = new Dictionary<string, Texture2D>();
            for (int i = 0; i < 4; i++)
            {
                Texture2D tex = bundle.AtlasLookup[i];
                textures.Add($"atlas{i}", tex);
            }

            return new Skin(textures, bundle.Name);
        }

        public static Skin LoadFromFile(string path)
        {
            return Deserialize(SkinBundle.LoadBundle(AssetBundle.LoadFromFile(path)));
        }
    }
    public class SkinManager {
        public static async Task<Skin> LoadAsync(params string[] selector)
        {
            return await Skin.DeserializeAsync(
                await SkinBundle.LoadBundleAsync(
                    await ResourceManager.LoadEmbeddedBundleAsync(
                        ResourceManager.SilkResolve(selector)
                    )
                ));
        }

        private static readonly string[] skins = [
            "red",
            "blue",
            "green",
            "purple"
        ];

        public static async Task<int> LoadLibrary()
        {
            int counter = 0;

            foreach (var skinName in skins)
            {
                Task<Skin> loadTask = LoadAsync("SkinLibrary", $"{skinName}.skin");
                await loadTask;
                if (loadTask.IsCompletedSuccessfully)
                {
                    Library[skinName] = loadTask.Result;
                    counter++;
                }
            }

            return counter;
        }
        public static Dictionary<string, Skin> Library { get; private set; } = new Dictionary<string, Skin>() { // finally made them embedded lmao
            //{ "red",  Skin.Deserialize(ResourceManager.LoadEmbedded(ResourceManager.SilkResolve("SkinLibrary", "red.skin")))},
            //{ "blue",  Skin.Deserialize(ResourceManager.LoadEmbedded(ResourceManager.SilkResolve("SkinLibrary", "blue.skin")))},
            //{ "green",  Skin.Deserialize(ResourceManager.LoadEmbedded(ResourceManager.SilkResolve("SkinLibrary", "green.skin")))},
            //{ "purple",  Skin.Deserialize(ResourceManager.LoadEmbedded(ResourceManager.SilkResolve("SkinLibrary", "purple.skin")))},
        };

        public static Skin Default
        {
            get
            {
                return Library["red"];
            }
        }

        public static Skin GetOrDefault(string name)
        {
            return Library.TryGetValue(name, out Skin skin) ? skin : Default;
        }

        public static void ApplySkin(tk2dSpriteCollectionData collection, Skin skin)
        {
            for (int i = 0; i < collection.textures.Length; i++)
            {
                string key = $"atlas{i}";

                if (!skin.Textures.TryGetValue(key, out Texture2D skinTex) || skinTex == null)
                    continue;

                Texture atlas = collection.textures[i];
                Texture2D readableAtlas = new(atlas.width, atlas.height, TextureFormat.RGBA32, false);

                RenderTexture rt = RenderTexture.GetTemporary(atlas.width, atlas.height, 0);
                Graphics.Blit(atlas, rt);
                RenderTexture.active = rt;
                readableAtlas.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                RenderTexture.active = null;
                RenderTexture.ReleaseTemporary(rt);

                collection.materials[i].SetTexture("_MainTex", skinTex);
                if (collection.materialInsts != null && collection.materialInsts.Length >= i)
                    collection.materialInsts[i].SetTexture("_MainTex", skinTex);
                collection.textures[i] = skinTex;
                //Logger.Msg("Applied texture", key);
            }
            //Logger.Msg("Applied skin:", collection.name, skin.SkinName);
        }

        #region Legacy Hue Shifting Methods

        //public static readonly Color capeShade = new Color(118, 45, 86);
        //public static readonly Color capeShade2 = new Color(80, 31, 59);
        public static readonly Color CAPE_PRIMARY = new(118f / 255f, 45f / 255f, 86f / 255f);

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

            if (h < 60) { rPrime = c; gPrime = x; } else if (h < 120) { rPrime = x; gPrime = c; } else if (h < 180) { gPrime = c; bPrime = x; } else if (h < 240) { gPrime = x; bPrime = c; } else if (h < 300) { rPrime = x; bPrime = c; } else { rPrime = c; bPrime = x; }

            r = (int) ((rPrime + m) * 255);
            g = (int) ((gPrime + m) * 255);
            b = (int) ((bPrime + m) * 255);
        }
        #endregion
    }
}
