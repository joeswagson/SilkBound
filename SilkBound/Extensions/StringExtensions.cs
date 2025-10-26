using SilkBound.Behaviours;
using SilkBound.Network;
using SilkBound.Utils;

namespace SilkBound.Extensions
{
    public static class StringExtensions
    {
        public static string ReplaceController(this string path, Weaver? target=null)
        {
            target ??= NetworkUtils.LocalClient;
            
            return path.Replace("Hero_Hornet(Clone)", HornetMirror.GetObjectName(target.ClientID));
        }
    }
}
