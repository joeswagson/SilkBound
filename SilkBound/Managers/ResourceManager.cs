using SilkBound.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;
using Logger = SilkBound.Utils.Logger;

namespace SilkBound.Managers
{
    public class ResourceManager
    {
        public static string SilkResolve(params string[] paths)
        {
            return "SilkBound.Resources." + string.Join(".", paths);
        }
        public static byte[] LoadEmbedded(string key, Assembly? target=null)
        {
            target ??= Assembly.GetExecutingAssembly();
            using MemoryStream ms = new MemoryStream();

            try
            {
                target.GetManifestResourceStream(key)?.CopyTo(ms);
            } catch(Exception ex)
            {
                Logger.Error($"Failed to load embedded resource '{key}': {ex}");
            }

            return ms.ToArray();
        }

        public class Resources
        {
            #region Helpers
            public class Casters
            {
                public static Sprite CastSprite(byte[] data)
                {
                    Texture2D tex = new Texture2D(2, 2);
                    tex.LoadImage(data);
                    return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                }
            }
            public class EmbeddedResource<T>(string key, Func<byte[], T> cast, Assembly? target = null)
            {
                private T? _resource = default;
                public T Resource
                {
                    get
                    {
                        return _resource ??= cast.Invoke(LoadEmbedded(key, target));
                    }
                }

                public bool TryGetResource(out T resource)
                {
                    resource = Resource;
                    return resource != null;
                }
            }
            #endregion
            #region Key Shortcuts
            public const string SKINLIBRARY = "SkinLibrary";
            public const string SOUNDS = "Sounds";
            public const string TEXTURES = "Textures";
            #endregion

            public static EmbeddedResource<Sprite> CustomTitle = new EmbeddedResource<Sprite>(SilkResolve(TEXTURES, "silkbound_logo.png"), Casters.CastSprite);
        }
    }
}
