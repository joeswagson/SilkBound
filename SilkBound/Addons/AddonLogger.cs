using MelonLoader;
using MelonLoader.Logging;

namespace SilkBound.Addons
{
    public class AddonLogger(string name)
    {
        public void Info(params object[] args)
        {
            MelonLogger.MsgDirect(ColorARGB.LightGray,$"[{name}] {string.Join(" ", args)}");
        }

        public void Warn(params object[] args)
        {
            MelonLogger.MsgDirect(ColorARGB.Yellow,$"[{name}] {string.Join(" ", args)}");
        }

        public void Error(params object[] args)
        {
            MelonLogger.MsgDirect(ColorARGB.IndianRed,$"[{name}] {string.Join(" ", args)}");
        }
    }
}