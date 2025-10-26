﻿#if MELON
using MelonLoader;
using MelonLoader.Logging;
using MelonLoader.Pastel;
#elif BEPIN
using BepInEx.Logging;
#endif
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace SilkBound.Utils
{
    // for when i port to bepin etc
    public class Logger
    {
#if BEPIN
        private static ManualLogSource _log = new("SilkBound");
#endif


        public static void Msg(params object?[] values)
        {
#if BEPIN
            _log.LogMessage(string.Join(" ", values));
#elif MELON
            MelonLogger.Msg($"{string.Join(" ", values)}");
#endif
        }

        public static void Debug(params object?[] values)
        {
#if BEPIN
            _log.LogMessage(string.Join(" ", values));
#elif MELON
            MelonLogger.Msg($"{"[".Pastel(ColorARGB.HotPink)}{"DEBUG".Pastel(ColorARGB.LightPink)}{"]".Pastel(ColorARGB.HotPink)} " + string.Join(" ", values));
#endif
        }

        public static void Warn(params object?[] values)
        {
#if BEPIN
            _log.LogWarning(string.Join(" ", values));
#elif MELON
            MelonLogger.Warning(string.Join(" ", values));
#endif
        }

        public static void Error(params object?[] values)
        {
#if BEPIN
            _log.LogError(string.Join(" ", values));
#elif MELON
            MelonLogger.Error(string.Join(" ", values));
#endif
        }

        public static void Stacktrace()
        {
            StackTrace trace = new(true);
            StackFrame[] frames = trace.GetFrames() ?? [];
            List<StackFrame> validFrames = [];
            foreach (var f in frames)
            {
                MethodBase method = f.GetMethod();
                if (method?.DeclaringType == null || method.DeclaringType == typeof(Logger)) continue;
                validFrames.Add(f);
            }

            foreach (var f in validFrames)
            {
                bool first = f == validFrames[0];
                MethodBase m = f.GetMethod();
                string methodInfo = m?.DeclaringType != null
                    ? $"{m.DeclaringType.FullName}.{m.Name}"
                    : "<unknown>";
                string fileInfo = f.HasSource()
                    ? $" at {f.GetFileName()}:{f.GetFileLineNumber()}"
                    : $"<{f.GetILOffset():X4}:{f.GetNativeOffset():X4}>";
                Msg(first ? "Stacktrace for method" : "-", methodInfo + fileInfo);
            }
        }
    }
}
