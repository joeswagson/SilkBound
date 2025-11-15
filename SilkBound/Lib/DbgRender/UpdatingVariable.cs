using HutongGames.PlayMaker.Actions;
using SilkBound.Types;
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
    public class UpdatingHostVariable<T> where T : class {
        public WeakReference Target { get; protected set; }
        public Func<T?, string> Serializer { get; }
        public Func<T?> Getter { get; }

        public UpdatingHostVariable(T? target, Func<T?>? getter = null, Func<T?, string>? serializer = null)
        {
            Target = new(target);
            Serializer = serializer ?? ToString;
            Getter = getter ?? Default;
        }

        private T? Default() { 
            return default;
        }
        private T? SafeAssign(T? target)
        {
            if (Target != null && Target.IsAlive)
                return (T) Target.Target;

            return Assign(target);
        }
        private T? Assign(T? target)
        {
            if (target == null)
                return (T) Target.Target;

            return (T) (Target = new(target)).Target;
        }
        private T? GetTarget()
        {
            // my codebase my rules
            if (Target.IsAlive && Target.Target is Server server && server.Disposed)
                return Assign(Getter());

            return SafeAssign(Getter());
        }
        public static string ToString(T? input) => input?.ToString() ?? "null";
        public override string ToString() => Serializer(GetTarget());
    }

}
