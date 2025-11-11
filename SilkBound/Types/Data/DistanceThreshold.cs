using System;
using System.Collections.Generic;
using System.Text;

namespace SilkBound.Types.Data {
    public static class Distances {
        public static float String(string a, string b) => a == b ? 0 : 1; // 1 = different, 0 = same
        public static float Float(float a, float b) => b - a;
        public static float FloatAbs(float a, float b) => Math.Abs(Float(a, b));
    }
    public class DistanceThreshold<T>(Func<T, T, float> distance, float threshold = 0.1f) where T : struct {
        public T? Cached;

        public T? Check(T current)
        {
            if (Cached == null || distance(Cached.Value, current) >= threshold)
            {
                Cached = current;
                return current;
            }

            return null;
        }
    }
}
