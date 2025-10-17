using MelonLoader;
using MelonLoader.Logging;
using MelonLoader.Pastel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net.Mail;
using System.Reflection;
using System.Text;

namespace SilkBound.Utils
{
    // for when i port to bepin etc
    public class Logger
    {
        public static void Msg(params object?[] values)
        {
            MelonLogger.Msg(string.Join(" ", values));
        }
        public static void Debug(params object?[] values)
        {
            if (!SilkConstants.DEBUG)
#pragma warning disable CS0162 // Unreachable code detected
                return;
#pragma warning restore CS0162

            MelonLogger.Msg($"{"[".Pastel(ColorARGB.HotPink)}{"DEBUG".Pastel(ColorARGB.LightPink)}{"]".Pastel(ColorARGB.HotPink)} " + string.Join(" ", values));
        }
        public static void Warn(params object?[] values)
        {
            MelonLogger.Warning(string.Join(" ", values));
        }
        public static void Error(params object?[] values)
        {
            MelonLogger.Error(string.Join(" ", values));
        }
        public static void Stacktrace()
        {
            StackTrace trace = new StackTrace(true);
            StackFrame[] frames = trace.GetFrames() ?? Array.Empty<StackFrame>();
            List<StackFrame> validFrames = new List<StackFrame>();
            foreach (var f in frames)
            {
                MethodBase method = f.GetMethod();
                if (method?.DeclaringType == null || method.DeclaringType == typeof(Logger)) continue;
                validFrames.Add(f);
            }
            foreach(var f in validFrames)
            {
                bool first = f == validFrames[0];
                MethodBase m = f.GetMethod();
                string methodInfo = m?.DeclaringType != null
                    ? $"{m.DeclaringType.FullName}.{m.Name}"
                    : "<unknown>";
                string fileInfo = f.HasSource() ? $" at {f.GetFileName()}:{f.GetFileLineNumber()}" : $"<{f.GetILOffset():X4}:{f.GetNativeOffset():X4}>";
                Msg(first ? "Stacktrace for method" : "-", methodInfo + fileInfo);
            }
        }
    }
}
