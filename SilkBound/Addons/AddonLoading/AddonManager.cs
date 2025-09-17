using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SilkBound.Addons.Events.Abstract;
using SilkBound.Addons.Events.Handlers;
using SilkBound.Utils;

namespace SilkBound.Addons.AddonLoading
{
    public static class AddonManager
    { 
        internal static Dictionary<string,AddonInfo> Addons = new();
        
        public static EventHandler<AddonInfo> AddonLoaded = (_, _) => {};
        public static EventHandler<AddonInfo[]> FinishedAddonLoading = (_, _) => {};
        public static EventHandler<AddonInfo> AddonUnloaded = (_, _) => {};

        internal static void LoadAddons()
        {
            if (ModFolder.Addons is null)
            {
                return;
            }

            foreach (var file in ModFolder.Addons.GetFiles("*.dll"))
            {
                var assembly = Assembly.LoadFrom(file.FullName);
                var types = assembly.GetTypes();
                var plugin = types.FirstOrDefault(type => type.IsAssignableFrom(typeof(SilkboundAddon)));
                if (plugin is null) continue;
                var addon = (SilkboundAddon)Activator.CreateInstance(plugin);
                var info = new AddonInfo(assembly, addon);
                Addons[addon.Name] = info;
                AddonLoaded.Invoke(null, info);
                addon.OnEnable();
                
                foreach (var type in assembly.GetTypes())
                {
                    foreach (var method in type.GetMethods())
                    {
                        var eventAttribute = method.GetCustomAttribute<RegisterEventAttribute>();
                        if (eventAttribute == null)
                        {
                            continue;
                        }

                        if (!method.IsStatic)
                        {
                            Logger.Error($"Event method {method.Name} in {type.Name} must be static.");
                            continue;
                        }

                        var parameters = method.GetParameters();
                        if (parameters.Length != 1 || !parameters[0].ParameterType.IsSubclassOf(typeof(SilkboundEvent)))
                        {
                            Logger.Error($"Invalid event registration method {method.Name} in {type.Name}");
                            continue;
                        }

                        var paramType = parameters[0].ParameterType;
                        EventManager.RegisterListener(paramType, method, eventAttribute.Priority);
                    }
                }
            }
            FinishedAddonLoading.Invoke(null, Addons.Values.ToArray());
        }

        internal static void UnloadAddons()
        {
            foreach (var addon in new List<AddonInfo>(Addons.Values))
            {
                addon.Addon.OnDisable();
                AddonUnloaded.Invoke(null, addon);
            }
            EventManager.Listeners.Clear();
            Addons.Clear();
        }
    }
}