using System;

namespace SilkBound.Utils
{
    public class Assertions {
        /// <summary>
        /// Scans elements for any that are null.
        /// </summary>
        /// <param name="elements">The elements to check.</param>
        /// <returns><see langword="true"/> if one of the elements was null; otherwise <see langword="false"/></returns>
        public static bool Null(params object?[] elements)
        {
            foreach (var element in elements)
                if (element == null)
                    return true;

            return false;
        }

        /// <summary>
        /// Checks if a list of elements are all null.
        /// </summary>
        /// <param name="elements">The elements to check.</param>
        /// <returns><see langword="true"/> if all of the elements are null; otherwise <see langword="false"/></returns>
        public static bool None(params object?[] elements)
        {
            foreach (var element in elements)
                if (element != null)
                    return false;

            return true;
        }

        public static bool EnsureLength(Array set, int goal)
        {
            return set.Length == goal;
        }
    }
}
