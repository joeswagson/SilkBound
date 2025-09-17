using System.Reflection;

namespace SilkBound.Addons.Events.Handlers;

public class ListenerInfo(MethodInfo method, EventPriority priority)
{
    public MethodInfo Method => method;
    public EventPriority Priority => priority;
}