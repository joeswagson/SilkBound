using SilkBound.Network.Packets;
using SilkBound.Types;
using SilkBound.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace SilkBound.Extensions {
    public static class PacketExtensions {
        public static bool HasLocalServer([NotNullWhen(true)] out NetworkServer? server)
        {
            server = NetworkUtils.LocalServer;
            return NetworkUtils.IsServer;
        }
        public static void Send(this Packet packet, NetworkConnection connection)
        {
            if (!HasLocalServer(out NetworkServer? server)) return;

            server.Send(packet);
        }
        public static void SendIncluding(this Packet packet, params NetworkConnection[] connections) => SendIncluding(packet, connections as IEnumerable<NetworkConnection>);
        public static void SendIncluding(this Packet packet, IEnumerable<NetworkConnection> connections)
        {
            if (!HasLocalServer(out NetworkServer? server)) return;

            server.SendIncluding(packet, connections);
        }
        public static void SendExcept(this Packet packet, params NetworkConnection[] connections) => SendExcept(packet, connections as IEnumerable<NetworkConnection>);
        public static void SendExcept(this Packet packet, IEnumerable<NetworkConnection> connections)
        {
            if (!HasLocalServer(out NetworkServer? server)) return;

            server.SendExcluding(packet, connections);
        }

        public static bool TryPack(this Packet packet, [NotNullWhen(true)] out byte[]? packetData)
        {
            return (packetData = PacketProtocol.PackPacket(packet)) != null;
        }
        public static bool TryUnpack(this byte[] packetData, [NotNullWhen(true)] out Packet? packet)
        {
            return (packet = PacketProtocol.UnpackPacket(packetData).Item3) != null;
        }
    }
}
