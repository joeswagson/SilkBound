using SilkBound.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SilkBound.Addons.Events;
using SilkBound.Addons.Events.Handlers;
using SilkBound.Utils;
using Logger = SilkBound.Utils.Logger;
using SilkBound.Managers;
using System.Runtime.Serialization; // for UnityMainThreadDispatcher

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


                //var tmp = (Packet)FormatterServices.GetUninitializedObject(attr.PacketType);
                var packetName = attr.PacketType.Name;

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
                string packetName = packet.GetType().Name;
                try
                {
                    if (connection is NetworkServer)
                        EventManager.CallEvent(new C2SPacketReceivedEvent(packet, connection));
                    else
                        EventManager.CallEvent(new S2CPacketReceivedEvent(packet, connection));
                }
                catch (Exception ex)
                {
                    Logger.Error($"Error firing events for {packetName}: {ex}");
                }

                if (TransactionManager.Fetch<bool>(packet) == true)
                {
                    Logger.Debug("Packet was cancelled.");
                    return;
                }

                if (Handlers.TryGetValue(packetName, out List<Action<Packet, NetworkConnection>> handlers))
                {
                    foreach (var handler in handlers.ToList())
                    {
                        try
                        {
                            handler(packet, connection);
                        }
                        catch (Exception ex)
                        {
                            Logger.Error($"Error in handler for {packetName}: {ex}");
                        }
                    }
                }
            };

            CoreLoop.InvokeOnGameThread(process);
            //ModMain.MainThreadDispatcher.Instance.Enqueue(process);
        }

        public abstract void Initialize();
    }
}
