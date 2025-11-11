using System;
using System.Collections.Generic;
using System.Text;

namespace SilkBound.Types.Data {
    public struct ComparableString(string? source = null) : IOptionalValue {
        private readonly string? Content = source;
        public readonly bool HasValue => Content != null;

        public static float Distance(ComparableString a, ComparableString b)
        {
            return string.Equals(a.Content, b.Content) ? 0 : 1;
        }

        public override int GetHashCode()
        {
            return Content?.GetHashCode() ?? 0;
        }

        public override readonly bool Equals(object? obj)
        {
            if (obj is null) return !HasValue;
            if (obj is ComparableString cs) return string.Equals(Content, cs.Content);
            if (obj is string s) return string.Equals(Content, s);
            return false;
        }

        public static bool operator ==(ComparableString left, object? right)
        {
            if (right is null) return !left.HasValue;
            return left.Equals(right);
        }

        public static bool operator !=(ComparableString left, object? right)
        {
            return !(left == right);
        }

        public static implicit operator string?(ComparableString source) => source.Content;
        public static implicit operator ComparableString(string? source) => new(source);
    }
}
