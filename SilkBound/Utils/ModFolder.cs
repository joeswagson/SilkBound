using System.IO;
using MelonLoader;
using MelonLoader.Utils;

namespace SilkBound.Utils
{
    internal static class ModFolder
    {
        private static readonly string _modFolderPath = MelonEnvironment.MelonBaseDirectory + "/Silkbound";
        private static readonly string _pluginsFolderPath = _modFolderPath + "/Addons";
        internal static DirectoryInfo? Instance
        {
            get;
            private set;
        }

        internal static DirectoryInfo? Addons
        {
            get;
            private set;
        }

        internal static void RegisterFolders()
        {
            Instance = Directory.CreateDirectory(_modFolderPath);
            Addons = Directory.CreateDirectory(_pluginsFolderPath);
        }
    }
}