using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SilkBound.Utils
{
    // for when i port to bepin etc
    public class Logger
    {
        public static void Msg(params object[] values)
        {
            MelonLogger.Msg(string.Join(", ", values));
        }
        public static void Warn(params object[] values)
        {
            MelonLogger.Warning(string.Join(", ", values));
        }
        public static void Error(params object[] values)
        {
            MelonLogger.Error(string.Join(", ", values));
        }
    }
}
