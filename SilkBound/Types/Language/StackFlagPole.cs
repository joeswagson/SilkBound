using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace SilkBound.Types.Language
{
    /// <summary>
    /// Must be used on the unity main thread.
    /// </summary>
    public sealed class StackFlag<T> : IDisposable
    {
        private static readonly AsyncLocal<int> depth = new();

        public static bool HasValue => Value != null;
        public static T? Value;
        public static bool Raised => depth.Value > 0;

        public StackFlag(T? value = default)
        {
            Value = value;
            depth.Value++;
        }

        public void Dispose()
        {
            depth.Value--;
            Value = default;
        }
    }
    /// <summary>
    /// Must be used on the unity main thread just like StackFlag<typeparamref name="T"/>.
    /// </summary>
    public sealed class StackFlagPole<T> : IDisposable
    {
        private static readonly AsyncLocal<int> depth = new();
        private static readonly List<StackFlagPole<T>> flagPoles = [];
        public static StackFlagPole<T> Tallest => flagPoles.First();
        public static StackFlagPole<T> Shortest => flagPoles.Last();

        public static bool HasValue => Value != null;
        public static T? Value;
        public static bool Raised => depth.Value > 0;

        public int Index { get; private set; } = -1; 
        public StackFlagPole(T? value = default)
        {
            Value = value;
            depth.Value++;
            Index = depth.Value - 1;
            flagPoles.Add(this);
        }

        public void Dispose()
        {
            flagPoles.Remove(this);
            depth.Value--;
            Value = default;
        }
    }
}
