using SilkBound;
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
            if (killed)
                Thread.Sleep(100);

            Process.Start($"{(DEBUG ? "F:\\! GAMES\\silksong\\Hollow-Knight-Silksong-SteamRIP.com\\Hollow Knight Silksong" : "F:\\SteamLibrary\\steamapps\\common\\Hollow Knight Silksong")}\\Hollow Knight Silksong.exe");
        }
    }
}
