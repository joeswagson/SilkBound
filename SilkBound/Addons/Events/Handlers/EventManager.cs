using System;
using System.Collections.Generic;
using System.Reflection;
using SilkBound.Addons.Events.Abstract;

namespace SilkBound.Addons.Events.Handlers;

internal static class EventManager
{
    internal static Dictionary<Type, List<ListenerInfo>> Listeners = [];
    internal static void CallEvent<T>(T @event) where T : SilkboundEvent
    {
        if(!Listeners.TryGetValue(typeof(T), out List<ListenerInfo> listeners))
            return;

        foreach (var info in listeners)
        {
            info.Method.Invoke(null, [@event]);
        }
    }

    internal static ListenerInfo RegisterListener(Type eventType, MethodInfo method, EventPriority priority)
    {
        ListenerInfo info = new(eventType, method, priority);

        List<ListenerInfo> infos = [.. Listeners[eventType], info];
        infos.Sort((x, y) => x.Priority.CompareTo(y.Priority));
        Listeners[eventType] = infos;

        return info;
    }
    internal static ListenerInfo Once(Type eventType, MethodInfo method, EventPriority priority)
    {
        ListenerInfo? info = null;

        void Wrapper(object @event)
        {
            method.Invoke(null, [@event]);
            UnregisterListener(info!);
        }

        var wrapperDelegate = (Action<object>)Wrapper;
        var wrapperMethod = wrapperDelegate.Method;

        info = RegisterListener(eventType, wrapperMethod, priority);
        return info;
    }

    internal static void UnregisterListener(ListenerInfo info)
    {
        if (!Listeners.TryGetValue(info.EventType, out List<ListenerInfo> listeners))
            return;

        Listeners[info.EventType].Remove(info);
    }

    internal static void UnregisterListener(Type eventType, Guid guid)
    {
        if (!Listeners.TryGetValue(eventType, out List<ListenerInfo> listeners))
            return;

        Listeners[eventType].RemoveAll(x => x.Guid == guid);
    }
}