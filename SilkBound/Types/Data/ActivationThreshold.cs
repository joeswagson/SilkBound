using System;
using System.Collections.Generic;
using System.Text;

namespace SilkBound.Types.Data {
    public static class Activators
    {
        public static bool StringEquals(string a, string b, float _) => a.Equals(b);
        public static bool StringEquals(ComparableString a, ComparableString b, float _) => a.Equals(b);
        public static bool GreaterThan(float _, float b, float threshold) => b > threshold;
        public static bool AbsGreaterThan(float _, float b, float threshold) => GreaterThan(_, Math.Abs(b), threshold);
        public static bool GreaterThanOrZero(float a , float b, float threshold) => b > threshold || (b == 0 && a != 0);
        public static bool AbsGreaterThanOrZero(float _, float b, float threshold) => GreaterThanOrZero(_, Math.Abs(b), threshold);
    }
    public class ActivationThreshold<T>(Func<T, T, float, bool> active, float threshold=0.1f) where T : struct {
        public T? Cached;

        public T? Check(T current)
        {
            if (Cached == null 
                || active(Cached.Value, current, threshold))
            {
                Cached = current;
                return current;
            }

            return null;
        }
    }
}
