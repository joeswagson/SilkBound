using System;

namespace SilkBound.Utils
{
    public class Assertions
    {
        public static bool EnsureLength(Array set, int goal)
        {
            return set.Length == goal;
        }
    }
}
