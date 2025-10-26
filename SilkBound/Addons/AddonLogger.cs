#if MELON
using MelonLoader;
#elif BEPIN
using BepInEx.Logging;
#endif

namespace SilkBound.Addons
{
    public class AddonLogger(string name)
    {
        #if BEPIN
        private readonly ManualLogSource _log = new(name);
        #endif
        
        public void Info(params object[] args)
        {
#if BEPIN
            _log.LogMessage($"[{name}] {string.Join(" ", args)}");
#elif MELON
            MelonLogger.Msg($"[{name}] {string.Join(" ", args)}");
#endif
        }

        public void Warn(params object[] args)
        {
#if BEPIN
            _log.LogWarning($"[{name}] {string.Join(" ", args)}");
#elif MELON
            MelonLogger.Warning($"[{name}] {string.Join(" ", args)}");
#endif
        }

        public void Error(params object[] args)
        {
#if BEPIN
            _log.LogError($"[{name}] {string.Join(" ", args)}");
#elif MELON
            MelonLogger.Error($"[{name}] {string.Join(" ", args)}");
#endif
        }
    }
}