using System;
using System.Collections.Generic;
using System.Text;

namespace SilkBound.Utils
{
    public class Assertions
    {
        public static bool EnsureLength(object[] set, int goal)
        {
            return set.Length == goal;
        }
    }
}
