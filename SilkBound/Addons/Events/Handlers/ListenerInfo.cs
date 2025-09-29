using System;
using System.Reflection;

namespace SilkBound.Addons.Events.Handlers;

public class ListenerInfo(Type eventType, MethodInfo method, EventPriority priority)
{
    public Type EventType => eventType;
    public MethodInfo Method => method;
    public EventPriority Priority => priority;

    private readonly Guid _guid = Guid.NewGuid();
    public Guid Guid => _guid;

}