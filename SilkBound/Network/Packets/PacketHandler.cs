using SilkBound.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using SilkBound.Addons.Events;
using SilkBound.Addons.Events.Handlers;
using SilkBound.Utils;

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

                var packetType = attr.PacketType;
                var packetInstance = (Packet)Activator.CreateInstance(packetType)!;
                var packetName = packetInstance.PacketName;

                Subscribe(packetName, (packet, conn) =>
                {
                    var parameters = method.GetParameters();

                    if (parameters.Length == 1 && typeof(Packet).IsAssignableFrom(parameters[0].ParameterType))
                    {
                        method.Invoke(this, new object[] { packet });
                    }
                    else if (parameters.Length == 2 && typeof(Packet).IsAssignableFrom(parameters[0].ParameterType) &&
                             typeof(NetworkConnection).IsAssignableFrom(parameters[1].ParameterType))
                    {
                        method.Invoke(this, new object[] { packet, conn });
                    }
                    else
                    {
                        throw new InvalidOperationException(
                            $"Packet handler {method.Name} must take (Packet) or (Packet, NetworkConnection | NetworkServer).");
                    }
                });
            }
        }

        public readonly Dictionary<string, List<Action<Packet, NetworkConnection>>> Handlers = new Dictionary<string, List<Action<Packet, NetworkConnection>>>(); // PacketName: [PacketNameHandler1, PacketNameHandler2]

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

            if (connection is NetworkServer)
                EventManager.CallEvent(new C2SPacketReceivedEvent(packet, connection));
            else if (Server.CurrentServer.Host == Server.CurrentServer.Connections.First(a => a.Connection == connection))
                EventManager.CallEvent(new S2CPacketReceivedEvent(packet, connection));
            else EventManager.CallEvent(new C2CPacketReceivedEvent(packet, connection));
            
            if (Handlers.TryGetValue(packet.PacketName, out List<Action<Packet, NetworkConnection>> handlers))
                foreach (var handler in handlers)
                    handler(packet, connection);
        }
        public abstract void Initialize();
    }
}
