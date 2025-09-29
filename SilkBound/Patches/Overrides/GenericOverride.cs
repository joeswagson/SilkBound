using HarmonyLib;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace SilkBound.Patches.Overrides
{

    public class GenericOverride<T> where T : class
    {
        public static void OverrideClass(Type overrideType)
        {
            Type baseType = typeof(T);

            var overrideMethods = overrideType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

            foreach (var method in overrideMethods)
            {
                var parameters = method.GetParameters();
                var paramTypes = parameters
                    .Select(p => p.ParameterType)
                    .ToArray();

                var baseMethod = baseType.GetMethod(method.Name,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    paramTypes,
                    null);

                if (baseMethod == null) continue;

                var dynamicPrefix = new HarmonyMethod(typeof(GenericOverride<T>)
                    .GetMethod(nameof(CreatePrefix), BindingFlags.NonPublic | BindingFlags.Static)
                    .MakeGenericMethod(baseMethod.ReturnType));

                Melon<ModMain>.Instance.HarmonyInstance.Patch(baseMethod, prefix: dynamicPrefix);
            }
        }

        private static bool CreatePrefix<R>(object __instance, object[] __args, ref R __result)
        {
            var overrideInstance = Activator.CreateInstance(typeof(GenericOverride<T>));

            var method = overrideInstance.GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                .FirstOrDefault(m => m.GetParameters().Length == __args.Length);

            if (method != null)
            {
                var res = method.Invoke(overrideInstance, __args);
                if (typeof(R) != typeof(void))
                    __result = (R)res;
            }

            return false;
        }
    }
}
