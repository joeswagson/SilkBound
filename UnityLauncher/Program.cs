using SilkBound;
using SilkBound.Utils;
using System.Diagnostics;

namespace UnityLauncher
{
    internal class Program
    {
        public const bool DEBUG =
#if DEBUG
            true;
#else
            false;
#endif
        static void Main(string[] args)
        {
            _ = ModMain.TypeName;

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

            string procPath = $"{(DEBUG ? "F:\\! GAMES\\silksong\\NoInstanceCheck\\Hollow Knight Silksong" : "F:\\SteamLibrary\\steamapps\\common\\Hollow Knight Silksong")}\\Hollow Knight Silksong.exe";
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
