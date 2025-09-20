using SilkBound.Network.Packets;
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

            // payload: [nameLen][name][payload]
            byte[] inner = new byte[1 + length + serialized.Length];
            inner[0] = length;
            Array.Copy(packetNameEncoded, 0, inner, 1, length);
            Array.Copy(serialized, 0, inner, 1 + length, serialized.Length);

            // packet frame size
            byte[] framed = new byte[4 + inner.Length];
            Array.Copy(BitConverter.GetBytes(inner.Length), 0, framed, 0, 4);
            Array.Copy(inner, 0, framed, 4, inner.Length);

            Logger.Debug("Framed Packet (send):", BitConverter.ToString(framed).Replace("-", ""), framed.Length);
            return framed;
        }

        public static (string?, Packet?) UnpackPacket(byte[] data)
        {
            Logger.Debug("Raw Packet (receive):", BitConverter.ToString(data).Replace("-", ""));
            try
            {
                using (MemoryStream stream = new MemoryStream(data))
                using (BinaryReader reader = new BinaryReader(stream, Encoding.UTF8))
                {
                    byte nameLength = reader.ReadByte();
                    byte[] nameBytes = reader.ReadBytes(nameLength);
                    string packetName = Encoding.UTF8.GetString(nameBytes);

                    byte[] payload = reader.ReadBytes((int)(stream.Length - stream.Position));

                    string[] validRoots =
                    {
                    "SilkBound.Network.Packets.Impl"
                };

                    var asm = Assembly.GetExecutingAssembly();
                    var type = asm.GetTypes()
                        .FirstOrDefault(t =>
                            t.Namespace != null &&
                            validRoots.Any(root =>
                                t.Namespace.StartsWith(root, StringComparison.Ordinal)) &&
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
