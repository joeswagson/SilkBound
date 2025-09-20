using SilkBound.Packets;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace SilkBound.Utils
{
    public class PacketProtocol
    {
        public static byte[]? PackPacket(Packet packet)
        {
            Logger.Msg("serializing");
            byte[] serialized = packet.Serialize();
            Logger.Msg("serialized");

            byte[] packetNameEncoded = Encoding.UTF8.GetBytes(packet.PacketName);
            if (packetNameEncoded.Length > byte.MaxValue)
            {
                Logger.Error("Packet", packet.PacketName, "has too long of a name to pack.");
                return null;
            }

            byte length = (byte)packetNameEncoded.Length;
            byte[] packetData = new byte[Math.Min(1 + length + serialized.Length, SilkConstants.PACKET_BUFFER)];

            // write name length
            packetData[0] = length;
            // write name
            Array.Copy(packetNameEncoded, 0, packetData, 1, length);
            // write data
            Array.Copy(serialized, 0, packetData, 1 + length, serialized.Length);

            Logger.Debug("Raw Packet (send):", BitConverter.ToString(packetData).Replace("-", ""), packetData.Length);
            return packetData;
        }

        public static (string?, Packet?) UnpackPacket(byte[] data)
        {
            Logger.Debug("Raw Packet (recieve):", BitConverter.ToString(data).Replace("-", ""));
            try
            {
                using (MemoryStream stream = new MemoryStream(data))
                using (BinaryReader reader = new BinaryReader(stream, Encoding.UTF8))
                {
                    byte nameLength = reader.ReadByte();
                    byte[] nameBytes = reader.ReadBytes(nameLength);
                    string packetName = Encoding.UTF8.GetString(nameBytes);

                    byte[] payload = reader.ReadBytes((int)(stream.Length - stream.Position));

                    string[] validNamespaces = // TODO: make this use sub namespaces. better for organizing the actual cs files for packets instead of one mega folder
                    {
                        "SilkBound.Packets.Impl",
                    };

                    var asm = Assembly.GetExecutingAssembly();
                    var type = asm.GetTypes()
                        .FirstOrDefault(t =>
                            validNamespaces.Contains(t.Namespace) &&
                            typeof(Packet).IsAssignableFrom(t) &&
                            string.Equals(
                                ((Packet)Activator.CreateInstance(t)!).PacketName,
                                packetName,
                                StringComparison.Ordinal));

                    if (type == null)
                    {
                        Logger.Error("UnpackPacket", $"Unknown packet type '{packetName}'");
                        return (packetName, null);
                    }

                    var tmp = (Packet)Activator.CreateInstance(type)!;
                    return (packetName, tmp.Deserialize(payload));
                }
            }
            catch (Exception ex)
            {
                Logger.Error("UnpackPacket", $"Failed to unpack: {ex}");
                return (null, null);
            }
        }
    }
}
