using SilkBound.Extensions;
using SilkBound.Utils;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
namespace SilkBound.Managers
{
    public class ResourceManager
    {
        public static string SilkResolve(params string[] paths)
        {
            return "SilkBound.Resources." + string.Join(".", paths);
        }
        public static byte[] LoadEmbedded(string key, Assembly? target = null)
        {
            var t = LoadEmbeddedAsync(key, target);
            return t.AssertResult(); // make nullsafe later (soooo laaaazy)
        }
        public static async Task<byte[]> LoadEmbeddedAsync(string key, Assembly? target = null)
        {
            target ??= Assembly.GetExecutingAssembly();
            using MemoryStream ms = new();

            try
            {
                using Stream? stream = target.GetManifestResourceStream(key);
                if (stream != null)
                    await stream.CopyToAsync(ms);
            } catch (Exception ex)
            {
                Logger.Error($"Failed to load embedded resource '{key}': {ex}");
            }

            return ms.ToArray();
        }
        public static async Task<AssetBundle> LoadEmbeddedBundleAsync(string key, Assembly? target = null)
        {
            target ??= Assembly.GetExecutingAssembly();
            using MemoryStream ms = new();

            try
            {
                using Stream? stream = target.GetManifestResourceStream(key);
                if (stream != null)
                    await stream.CopyToAsync(ms);
            } catch (Exception ex)
            {
                Logger.Error($"Failed to load embedded resource '{key}': {ex}");
            }

            AssetBundleCreateRequest req = AssetBundle.LoadFromMemoryAsync(ms.ToArray());
            await req;
            return req.assetBundle;
        }

        public class Resources
        {
            #region Helpers
            public class Casters
            {
                public static Sprite CastSprite(byte[] data)
                {
                    Texture2D tex = new(2, 2);
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

            public class BundledResource<T>(AssetBundle bundle, string name) where T : Object
            {
                public T? _object;
                public T? Object => NetworkUtils.IsNullPtr(_object) ? _object = bundle.LoadAsset<T>(name) : _object; // not using ??= because (iirc) it doesnt trigger unitys dead native object check
            }
            #endregion
            #region Key Shortcuts
            public const string SKINLIBRARY = "SkinLibrary";
            public const string SOUNDS = "Sounds";
            public const string TEXTURES = "Textures";
            #endregion

            public static EmbeddedResource<Sprite> CustomTitle = new(SilkResolve(TEXTURES, "silkbound_logo.png"), Casters.CastSprite);
        }
    }
}
