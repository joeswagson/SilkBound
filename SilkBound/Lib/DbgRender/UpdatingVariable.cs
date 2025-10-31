using System;
using System.Collections.Generic;
using System.Text;

namespace SilkBound.Lib.DbgRender {
    public class UpdatingVariable<T> {
        public T Target { get; }
        public Func<T, string> Serializer { get; }

        public UpdatingVariable(T target, Func<T, string>? serializer = null)
        {
            Target = target;
            Serializer = serializer ?? ToString;
        }

        public static string ToString(T input) => input?.ToString() ?? "null";
        public override string ToString() => Serializer(Target);
    }
    public class UpdatingHostVariable<T> {
        public T? Target { get; protected set; }
        public Func<T?, string> Serializer { get; }
        public Func<T?> Getter { get; }

        public UpdatingHostVariable(T? target, Func<T?>? getter = null, Func<T?, string>? serializer = null)
        {
            Target = target;
            Serializer = serializer ?? ToString;
            Getter = getter ?? Default;
        }

        private T? Default() { 
            return default; 
        }
        public static string ToString(T? input) => input?.ToString() ?? "null";
        public override string ToString() => Serializer(Target ??= Getter());
    }

}
