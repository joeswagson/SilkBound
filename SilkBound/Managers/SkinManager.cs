using SilkBound.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using UnityEngine;
using static MelonLoader.MelonLogger;

namespace SilkBound.Managers
{

    public class Skin
    {
        public Dictionary<string, Texture2D> Textures { get; private set; }
        public string SkinName { get; private set; }

        public Skin(Dictionary<string, Texture2D> textures, string skinName = "Unnamed")
        {
            Textures = textures;
            SkinName = skinName;
        }

        public static Skin LoadFromFolder(string path)
        {
            var textures = new Dictionary<string, Texture2D>();

            for (int i = 0; i <= 3; i++)
            {
                string filename = $"atlas{i}.png";
                string filePath = Path.Combine(path, filename);

                if (File.Exists(filePath))
                {
                    Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    tex.LoadImage(File.ReadAllBytes(filePath));
                    tex.Apply();
                    textures.Add(Path.GetFileNameWithoutExtension(filename), tex);
                }
            }

            return new Skin(textures, Path.GetFileName(path));
        }

        public static byte[] Serialize(Skin skin)
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(ms, Encoding.UTF8))
            {
                writer.Write(MagicByteManager.SKIN_SIGNATURE);

                writer.Write(skin.SkinName);
                writer.Write(skin.Textures.Count);

                int index = 0;
                foreach (var kvp in skin.Textures)
                {
                    byte[] pngData = kvp.Value.EncodeToPNG();
                    writer.Write(pngData.Length);
                    writer.Write(pngData);
                    index++;
                }

                return ms.ToArray();
            }
        }

        public static Skin Deserialize(byte[] data)
        {
            using (MemoryStream ms = new MemoryStream(data))
            using (BinaryReader reader = new BinaryReader(ms, Encoding.UTF8))
            {
                reader.ReadBytes(MagicByteManager.SKIN_SIGNATURE.Length);

                string skinName = reader.ReadString();
                int textureCount = reader.ReadInt32();

                var textures = new Dictionary<string, Texture2D>();
                for (int i = 0; i < textureCount; i++)
                {
                    int sectionLength = reader.ReadInt32();
                    byte[] sectionData = reader.ReadBytes(sectionLength);

                    Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    tex.LoadImage(sectionData);
                    tex.Apply();

                    textures.Add($"atlas{i}", tex);
                }

                return new Skin(textures, skinName);
            }
        }

        public static Skin LoadFromFile(string path)
        {
            return Deserialize(File.ReadAllBytes(path));
        }

        public static void WriteToFile(Skin skin, string path)
        {
            File.WriteAllBytes(path, Serialize(skin));
        }

        public byte[] Serialize() => Serialize(this);
        public void WriteToFile(string path) => WriteToFile(this, path);
    }
    public class SkinManager
    {
        public static Dictionary<string, Skin> Library { get; private set; } = new Dictionary<string, Skin>() { // finally made them embedded lmao
            { "red",  Skin.Deserialize(ResourceManager.LoadEmbedded(ResourceManager.SilkResolve("SkinLibrary", "red.skin")))},
            { "blue",  Skin.Deserialize(ResourceManager.LoadEmbedded(ResourceManager.SilkResolve("SkinLibrary", "blue.skin")))},
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

        public static void ApplySkin(tk2dBaseSprite sprite, Skin skin)
        {
            var collection = sprite.Collection.textures;

            for (int i = 0; i < collection.Length; i++)
            {
                string key = $"atlas{i}";

                if (!skin.Textures.TryGetValue(key, out Texture2D skinTex))
                    continue;

                Texture atlas = collection[i];
                Texture2D readableAtlas = new Texture2D(atlas.width, atlas.height, TextureFormat.RGBA32, false);

                RenderTexture rt = RenderTexture.GetTemporary(atlas.width, atlas.height, 0);
                Graphics.Blit(atlas, rt);
                RenderTexture.active = rt;
                readableAtlas.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                RenderTexture.active = null;
                RenderTexture.ReleaseTemporary(rt);

                sprite.Collection.materialInsts[i].SetTexture("_MainTex", skinTex);
                sprite.Collection.materials[i].SetTexture("_MainTex", skinTex);
                collection[i] = skinTex;
            }
        }

        #region Legacy Hue Shifting Methods

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
        #endregion
    }
}
