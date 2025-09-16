using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace SilkBound.Managers
{
    public class Skin
    {
        public Dictionary<string, Texture2D> Textures { get; private set; }
        public Skin(Dictionary<string, Texture2D> texture)
        {
            Textures = texture;
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
                    Texture2D tex = new Texture2D(4096, 4096, TextureFormat.RGBA32, false);
                    tex.LoadImage(File.ReadAllBytes(filePath));
                    tex.Apply();
                    textures.Add(Path.GetFileNameWithoutExtension(filename), tex);
                }
            }

            return new Skin(textures);
        }
    }
    public class SkinManager
    {
        public static Dictionary<string, Skin> Library { get; private set; } = new Dictionary<string, Skin>() {
            { "red",  Skin.LoadFromFolder("C:\\Users\\Joe\\OneDrive\\Desktop\\code\\silksong\\red")},
            { "blue",  Skin.LoadFromFolder("C:\\Users\\Joe\\OneDrive\\Desktop\\code\\silksong\\blue")}
        };
    }
}
