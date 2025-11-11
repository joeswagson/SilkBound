using SilkBound.Processors;
using SilkBound.Processors.Impl;
using System;
using System.Collections.Generic;
using System.Text;

namespace SilkBound.Managers {
    public static class ProcessorManager {
        private static readonly Dictionary<Type, object> map = new();

        private static Processor<T>? Create<T>()
        {
            var t = typeof(T);
            if (!t.IsGenericType || t.GetGenericTypeDefinition() != typeof(Processor<>))
                return default;

            var inst = Activator.CreateInstance<Processor<T>>();

            if (inst != null)
            {
                map.Add(t, inst);
                inst.Init();
            }

            return inst;
        }
        internal static void Initialize()
        {
            Create<HeroControllerProcessor>();
        }

        public static void Register<T>(T instance) where T : class
        {
            map[typeof(T)] = instance;
        }

        public static T Get<T>() where T : class
        {
            return (T) map[typeof(T)];
        }
    }
}
