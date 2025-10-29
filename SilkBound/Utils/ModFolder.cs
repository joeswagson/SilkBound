using System;
using System.IO;
#if MELON
using MelonLoader.Utils;
#elif BEPIN
using BepInEx;
#endif

namespace SilkBound.Utils
{
    internal static class ModFolder
    {
        private static readonly string _rootFolderPath = 
            #if MELON
            MelonEnvironment.MelonBaseDirectory + "/Silkbound";
            #elif SERVER
            AppDomain.CurrentDomain.BaseDirectory;
            #elif BEPIN
            Paths.PluginPath + "/Silkbound";
            #endif
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