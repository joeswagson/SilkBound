using SilkBound.Addons.Events;
using SilkBound.Addons.Events.Handlers;
using SilkBound.Managers;
using SilkBound.Network.NetworkLayers;
using SilkBound.Network.Packets.Handlers; // for UnityMainThreadDispatcher
using SilkBound.Types.Language;
using SilkBound.Types.Language.FlagContexts;
using SilkBound.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SilkBound.Network.Packets
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class PacketHandlerAttribute(Type packetType) : Attribute
    {
        public Type PacketType { get; } = packetType;
    }

    public abstract class PacketHandler
    {
        public PacketHandler()
        {
            var methods = GetType().GetMethods(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            var packetTypes = PacketProtocol.GetPacketTypes();

            bool isClient = this is ClientPacketHandler;
            bool isServer = this is ServerPacketHandler;

            foreach (var packetType in packetTypes)
                if (isClient && packetType.GetMethod(nameof(Packet.ClientHandler)).DeclaringType == packetType)
                    Subscribe(packetType.Name, (packet, connection) => packet.ClientHandler(connection));
                else if (isServer && packetType.GetMethod(nameof(Packet.ServerHandler)).DeclaringType == packetType)
                    Subscribe(packetType.Name, (packet, connection) => packet.ServerHandler(connection));
                else if (!isClient && !isServer && NetworkUtils.IsServer)
                    Subscribe(packetType.Name, (packet, connection) => packet.Relay(connection));

            foreach (var method in methods)
            {
                var attr = method.GetCustomAttribute<PacketHandlerAttribute>();
                if (attr == null) continue;

                var packetName = attr.PacketType.Name;

                Subscribe(packetName, (packet, conn) =>
                {
                    var parameters = method.GetParameters();

                    if (parameters.Length == 1 && typeof(Packet).IsAssignableFrom(parameters[0].ParameterType))
                    {
                        method.Invoke(this, [packet]);
                    }
                    else if (parameters.Length == 2 &&
                             typeof(Packet).IsAssignableFrom(parameters[0].ParameterType) &&
                             (typeof(NetworkConnection).IsAssignableFrom(parameters[1].ParameterType) ||
                              typeof(NetworkServer).IsAssignableFrom(parameters[1].ParameterType)))
                    {
                        method.Invoke(this, [packet, conn]);
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
            = []; // PacketName -> Handlers

        public void Subscribe(string packetName, Action<Packet, NetworkConnection> handler)
        {
            if (!Handlers.ContainsKey(packetName))
            {
                Handlers.Add(packetName, []);
            }

            Handlers[packetName].Add(handler);
        }

        public void Handle(Packet? packet, NetworkConnection connection)
        {
            if (packet == null) return;

            void process()
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
                            using (new StackFlag<PacketHandlerContext>(
                                       new()
                                       {
                                           Packet = packet,
                                           Sender = packet.Sender,
                                           Connection = connection
                                       }))
                                handler(packet, connection);
                        }
                        catch (Exception ex)
                        {
                            Logger.Error($"Error in handler for {packetName}: {ex}");
                        }
                    }
                }
            }

#if SERVER
            process();
#else
            CoreLoop.InvokeOnGameThread(process);
#endif
            //ModMain.MainThreadDispatcher.Instance.Enqueue(process);
        }

        public abstract void Initialize();
    }
}