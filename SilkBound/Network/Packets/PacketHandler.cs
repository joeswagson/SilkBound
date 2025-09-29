using SilkBound.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SilkBound.Addons.Events;
using SilkBound.Addons.Events.Handlers;
using SilkBound.Utils;
using UnityEngine;
using Logger = SilkBound.Utils.Logger;
using SilkBound.Managers; // for UnityMainThreadDispatcher

namespace SilkBound.Network.Packets
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class PacketHandlerAttribute : Attribute
    {
        public Type PacketType { get; }

        public PacketHandlerAttribute(Type packetType)
        {
            PacketType = packetType;
        }
    }

    public abstract class PacketHandler
    {
        public PacketHandler()
        {
            var methods = GetType().GetMethods(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (var method in methods)
            {
                var attr = method.GetCustomAttribute<PacketHandlerAttribute>();
                if (attr == null) continue;

                Packet? packetInstance = null;
                try
                {
                    packetInstance = (Packet)Activator.CreateInstance(attr.PacketType)!;
                }
                catch (Exception ex)
                {
                    Logger.Error($"Failed to create packet instance for {attr.PacketType}: {ex}");
                    continue;
                }

                var packetName = packetInstance.PacketName;

                Subscribe(packetName, (packet, conn) =>
                {
                    var parameters = method.GetParameters();

                    if (parameters.Length == 1 && typeof(Packet).IsAssignableFrom(parameters[0].ParameterType))
                    {
                        method.Invoke(this, new object[] { packet });
                    }
                    else if (parameters.Length == 2 &&
                             typeof(Packet).IsAssignableFrom(parameters[0].ParameterType) &&
                             (typeof(NetworkConnection).IsAssignableFrom(parameters[1].ParameterType) || typeof(NetworkServer).IsAssignableFrom(parameters[1].ParameterType)))
                    {
                        method.Invoke(this, new object[] { packet, conn });
                    }
                    else
                    {
                        Logger.Error(
                            $"Packet handler {method.Name} signature invalid. Must take (Packet) or (Packet, NetworkConnection).");
                    }
                });
            }
        }

        public readonly Dictionary<string, List<Action<Packet, NetworkConnection>>> Handlers
            = new Dictionary<string, List<Action<Packet, NetworkConnection>>>(); // PacketName -> Handlers

        public void Subscribe(string packetName, Action<Packet, NetworkConnection> handler)
        {
            if (!Handlers.ContainsKey(packetName))
            {
                Handlers.Add(packetName, new List<Action<Packet, NetworkConnection>>());
            }

            Handlers[packetName].Add(handler);
        }

        public void Handle(Packet? packet, NetworkConnection connection)
        {
            if (packet == null) return;

            Action process = () =>
            {
                try
                {
                    if (connection is NetworkServer)
                        EventManager.CallEvent(new C2SPacketReceivedEvent(packet, connection));
                    else
                        EventManager.CallEvent(new S2CPacketReceivedEvent(packet, connection));
                }
                catch (Exception ex)
                {
                    Logger.Error($"Error firing events for {packet.PacketName}: {ex}");
                }

                if (TransactionManager.Fetch<bool>(packet) == true)
                {
                    Logger.Debug("Packet was cancelled.");
                    return;
                }

                if (Handlers.TryGetValue(packet.PacketName, out List<Action<Packet, NetworkConnection>> handlers))
                {
                    foreach (var handler in handlers.ToList())
                    {
                        try
                        {
                            handler(packet, connection);
                        }
                        catch (Exception ex)
                        {
                            Logger.Error($"Error in handler for {packet.PacketName}: {ex}");
                        }
                    }
                }
            };

            ModMain.MainThreadDispatcher.Instance.Enqueue(process);
        }

        public abstract void Initialize();
    }
}
