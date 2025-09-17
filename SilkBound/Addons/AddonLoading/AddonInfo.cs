using System.Reflection;

namespace SilkBound.Addons.AddonLoading
{
    public class AddonInfo(Assembly assembly, SilkboundAddon addon)
    {
        public SilkboundAddon Addon = addon;
        public Assembly Assembly = assembly;
    }
    
}