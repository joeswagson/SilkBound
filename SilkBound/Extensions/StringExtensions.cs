using SilkBound.Behaviours;
using SilkBound.Network;
using SilkBound.Utils;
using System.Linq;

namespace SilkBound.Extensions {
    public static class CharacterSets {
        public static char[] ATOZLOWER = [.. "abcdefghijklmnopqrstuvwxyz"];
        public static char[] ATOZUPPER = [.. "ABCDEFGHIJKLMNOPQRSTUVWXYZ"];
        public static char[] ATOZ = [.. ATOZLOWER, .. ATOZUPPER];

        public static char[] NUMBERS = [.. "0123456789"];
        public static char[] SPECIAL = [.. "!@#$%^&*()"];

        public static char[] ALPHANUMERIC = [.. ATOZ, .. NUMBERS];
        public static char[] PRINTABLE = [.. ALPHANUMERIC, .. SPECIAL];
    }
    public static class StringExtensions {
        public static string ReplaceController(this string path, Weaver? target = null)
        {
            target ??= NetworkUtils.LocalClient;

            return path.Replace("Hero_Hornet(Clone)", HornetMirror.GetObjectName(target.ClientID));
        }
        public static string Sanitize(this string src, char[] allowed) => new string([.. src.Where(c => allowed.Contains(c))]);
    }
}