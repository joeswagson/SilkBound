using MelonLoader.Utils;
using System;
#if DEBUG
using System.Runtime.InteropServices;
#endif
using System.Text.RegularExpressions;
using Unity.Mathematics;
using UnityEngine;

namespace SilkBound.Utils
{
    public class SilkDebug
    {
        public static int GetClientNumber()
        {
            string exeName = MelonEnvironment.GameExecutableName;
            //Logger.Msg(exeName);
            if (exeName.Equals("Hollow Knight Silksong", StringComparison.OrdinalIgnoreCase))
                return 1;

            var match = Regex.Match(exeName, @"\.Client(\d+)$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (match.Success && int.TryParse(match.Groups[1].Value, out int num))
                return num;

            return 0;
        }

#if DEBUG
        private delegate bool EnumThreadDelegate(IntPtr hWnd, IntPtr lParam);
        [DllImport("user32.dll")]
        static extern bool EnumThreadWindows(int dwThreadId, EnumThreadDelegate lpfn, IntPtr lParam);

        [DllImport("Kernel32.dll")]
        static extern int GetCurrentThreadId();

        static IntPtr GetWindowHandle()
        {
            IntPtr returnHwnd = IntPtr.Zero;
            var threadId = GetCurrentThreadId();
            EnumThreadWindows(threadId,
                (hWnd, lParam) => {
                    if (returnHwnd == IntPtr.Zero) returnHwnd = hWnd;
                    return true;
                }, IntPtr.Zero);
            return returnHwnd;
        }
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll", EntryPoint = "SetWindowText")]
        public static extern bool SetWindowText(System.IntPtr hwnd, System.String lpString);
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_NOACTIVATE = 0x0010;
        public static void SetWindowPositionAndSize(IntPtr hWnd, int x, int y, int width, int height)
        {
            SetWindowPos(hWnd, IntPtr.Zero, x, y, width, height, SWP_NOZORDER | SWP_NOACTIVATE);
        }
        public static void PositionGameWindow(Vector2Int origin, Vector2Int windowSize, int w)
        {
            //Screen.SetResolution(windowSize.x, windowSize.y, false);

            int i = GetClientNumber();

            int pX = 10;
            int pY = 10;

            int x = (i - 1) % w;
            int y = -(((int)math.ceil((float)i / (float)w)) - 1);

            Vector2Int newPos = new(origin.x + x * windowSize.x + pX, origin.y - y * windowSize.y + pY);

            Logger.Msg("Moved window:", i, x, y, newPos.x, newPos.y, windowSize.x, windowSize.y);

            SetWindowPositionAndSize(GetWindowHandle(), newPos.x, newPos.y, windowSize.x, windowSize.y);
        }
        public static void PositionConsoleWindow(Vector2Int origin, Vector2Int windowSize, int w)
        {
            IntPtr consoleHandle = GetConsoleWindow();
            if (consoleHandle == IntPtr.Zero)
            {
                Logger.Warn("No console window found.");
                return;
            }

            int i = GetClientNumber();

            int pX = 10;
            int pY = 10;

            int x = (i - 1) % w;
            int y = -(((int)math.ceil((float)i / (float)w)) - 1);

            Vector2Int newPos = new(origin.x + x * windowSize.x + pX, origin.y - y * windowSize.y + pY);

            Logger.Msg("Moved console:", i, x, y, newPos.x, newPos.y, windowSize.x, windowSize.y);

            SetWindowPositionAndSize(consoleHandle, newPos.x, newPos.y, windowSize.x, windowSize.y);
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetConsoleWindow();
#endif
    }
}
