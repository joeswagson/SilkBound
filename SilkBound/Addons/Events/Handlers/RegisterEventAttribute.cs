using System;

namespace SilkBound.Addons.Events.Handlers;

[AttributeUsage(AttributeTargets.Method)]
public class RegisterEventAttribute(EventPriority priority = EventPriority.Normal) : Attribute
{
    public EventPriority Priority { get; } = priority;
}