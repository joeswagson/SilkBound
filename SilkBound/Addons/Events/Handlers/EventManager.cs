using System;
using System.Collections.Generic;
using System.Reflection;
using SilkBound.Addons.Events.Abstract;

namespace SilkBound.Addons.Events.Handlers;

internal static class EventManager
{
    internal static Dictionary<Type, List<ListenerInfo>> Listeners = new();
    internal static void CallEvent<T>(T @event) where T : SilkboundEvent
    {
        if(!Listeners.TryGetValue(typeof(T), out List<ListenerInfo> listeners))
            return;

        foreach (var info in listeners)
        {
            info.Method.Invoke(null, [@event]);
        }
    }

    internal static void RegisterListener(Type eventType, MethodInfo method, EventPriority priority)
    {
        List<ListenerInfo> infos = new(Listeners[eventType]);
        infos.Add(new ListenerInfo(method, priority));
        infos.Sort((x, y) => x.Priority.CompareTo(y.Priority));
        Listeners[eventType] = infos;
    }
    
}