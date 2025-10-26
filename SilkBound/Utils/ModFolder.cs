using System.IO;
using MelonLoader.Utils;

namespace SilkBound.Utils
{
    internal static class ModFolder
    {
        private static readonly string _rootFolderPath = MelonEnvironment.MelonBaseDirectory + "/Silkbound";
        private static readonly string _pluginsFolderPath = _rootFolderPath + "/Addons";
        private static readonly string _savesFolderPath = _rootFolderPath + "/Saves";
        internal static DirectoryInfo Root
        {
            get;
            private set;
        }

        internal static DirectoryInfo Addons
        {
            get;
            private set;
        }

        internal static DirectoryInfo Saves
        {
            get;
            private set;
        }

        static ModFolder()
        {
            Root = Directory.CreateDirectory(_rootFolderPath);
            Addons = Directory.CreateDirectory(_pluginsFolderPath);
            Saves = Directory.CreateDirectory(_savesFolderPath);
        }
    }
}