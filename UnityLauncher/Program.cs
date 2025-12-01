using SilkBound;
using SilkBound.Utils;
using System.Diagnostics;

namespace UnityLauncher {
    internal class Program {
        public const bool DEBUG =
#if DEBUG
            true;
#else
            false;
#endif
        private enum Loader {
            MelonLoader,
            BepInEx
        }
        private enum BootstrapperState {
            NotFound,
            Disabled,
            Enabled
        }
        private static Dictionary<Loader, string> loaderBootstrappers = new() {
            { Loader.MelonLoader, "version.dll" },
            { Loader.BepInEx, "winhttp.dll" }
        };

        static BootstrapperState FindBootstrapper(Loader loader, string gamePath)
        {
            string bootStrapper = Path.Combine(
                gamePath,
                loaderBootstrappers[loader]);

            bool hasEnabled = File.Exists(bootStrapper);
            bool hasDisabled = File.Exists(ConvertDll(bootStrapper, false));

            if (hasEnabled || hasDisabled)
                return hasEnabled ? BootstrapperState.Enabled : BootstrapperState.Disabled;

            return BootstrapperState.NotFound;
        }
        static string ConvertDll(string path, bool enabled = true)
        {
            return Path.ChangeExtension(path, enabled ? "dll" : "bak");
        }

        static void UpdateBootstrapper(Loader loader, string gamePath, BootstrapperState? state = null)
        {
            state ??= FindBootstrapper(loader, gamePath);

            switch (state)
            {
                case BootstrapperState.Enabled:
                    string bootStrapper = Path.Combine(
                        gamePath,
                        loaderBootstrappers[loader]);

                    if (FindBootstrapper(loader, gamePath) == BootstrapperState.Disabled)
                        File.Move(
                            ConvertDll(bootStrapper, false),
                            ConvertDll(bootStrapper, true));
                    break;
                case BootstrapperState.Disabled:
                    string disabledBootStrapper = Path.Combine(
                        gamePath,
                        loaderBootstrappers[loader]);
                    if (FindBootstrapper(loader, gamePath) == BootstrapperState.Enabled)
                        File.Move(
                            ConvertDll(disabledBootStrapper, true),
                            ConvertDll(disabledBootStrapper, false));
                    break;
                case BootstrapperState.NotFound:
                default:
                    break;
            }
        }
        static void DisableLoaders(string gamePath)
        {
            foreach (var loader in loaderBootstrappers.Keys)
                UpdateBootstrapper(loader, gamePath, BootstrapperState.Disabled);
        }

        static bool ApplyBootstrapper(Loader loader, string gamePath)
        {
            DisableLoaders(gamePath);

            string bootStrapper = Path.Combine(
                gamePath,
                loaderBootstrappers[loader]);

            var state = FindBootstrapper(loader, gamePath);
            Debug.WriteLine($"State: {state.ToString()}");
            if (state == BootstrapperState.Enabled)
                return true;
            if (state == BootstrapperState.Disabled)
            {
                UpdateBootstrapper(loader, gamePath, BootstrapperState.Enabled);
                return true;
            }

            return false;
        }

        static void Main(string[] args)
        {
            var loader =
#if MELON
                Loader.MelonLoader; // MelonLoader
#elif BEPINEX
                Loader.BepInEx; // BepInEx
#else
                Loader.MelonLoader; // Default (MelonLoader)
#endif


            string props = File.ReadAllText("../../../../Silkbound/local.props");
            string offline = props.Split("<SilksongPath_Offline>")[1].Split("</SilksongPath_Offline>")[0].Trim();
            string steam = props.Split("<SilksongPath_Steam>")[1].Split("</SilksongPath_Steam>")[0].Trim();
            _ = typeof(Silkbound);

            string gamePath = DEBUG ? offline : steam;
            Debug.WriteLine("{0}, {1}", loader, gamePath);
            ApplyBootstrapper(loader, gamePath);

            bool killed = false;
            foreach (Process p in Process.GetProcessesByName("Hollow Knight Silksong"))
            {
                p.Kill();
                killed = true;
            }
            for (int i = 2; i <= 10; i++)
            {
                Console.WriteLine($"Checking for Hollow Knight Silksong.Client{i}");
                foreach (Process p in Process.GetProcessesByName($"Hollow Knight Silksong.Client{i}"))
                {
                    p.Kill();
                    killed = true;
                }
            }
            if (killed)
                Thread.Sleep(100);

            string procPath = $"{gamePath}\\Hollow Knight Silksong.exe";//"F:\\! GAMES\\silksong\\NoInstanceCheck\\Hollow Knight Silksong" : "F:\\SteamLibrary\\steamapps\\common\\Hollow Knight Silksong")}\\Hollow Knight Silksong.exe";
            Process.Start(procPath);
            if (DEBUG)
            {
                Thread.Sleep(500);
                if (SilkConstants.TEST_CLIENTS > 1)
                    for (int i = 2; i <= SilkConstants.TEST_CLIENTS; i++)
                        Process.Start(procPath.Replace("Silksong.exe", $"Silksong.Client{i}.exe"));
            }
        }
    }
}
