using SilkBound.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

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
    }
}
