using SilkBound.Network.Packets;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

namespace SilkBound.Utils
{
    public class PacketProtocol
    {
        public static byte[]? PackPacket(Packet packet)
        {
            using MemoryStream ms = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(ms, Encoding.UTF8);

            // serialize packet data
            packet.SerializeInternal(writer);
            byte[] serialized = ms.ToArray();

            // encode packet name
            string packetName = packet.GetType().Name;
            byte[] packetNameEncoded = Encoding.UTF8.GetBytes(packetName);
            if (packetNameEncoded.Length > byte.MaxValue)
            {
                Logger.Error("Packet", packetName, "has too long of a name to pack.");
                return null;
            }

            byte nameLength = (byte)packetNameEncoded.Length;

            // encode clientId
            byte[] clientIdBytes = packet.Sender.ClientID.ToByteArray();//NetworkUtils.ClientID.ToByteArray();

            // payload: [nameLen][name][clientId][payload]
            byte[] inner = new byte[1 + nameLength + clientIdBytes.Length + serialized.Length];
            inner[0] = nameLength;
            Array.Copy(packetNameEncoded, 0, inner, 1, nameLength);
            Array.Copy(clientIdBytes, 0, inner, 1 + nameLength, clientIdBytes.Length);
            Array.Copy(serialized, 0, inner, 1 + nameLength + clientIdBytes.Length, serialized.Length);

            // prepend size
            byte[] framed = new byte[4 + inner.Length];
            Array.Copy(BitConverter.GetBytes(inner.Length), 0, framed, 0, 4);
            Array.Copy(inner, 0, framed, 4, inner.Length);

            return framed;
        }

        public static (string?, Guid?, Packet?) UnpackPacket(byte[] data)
        {
            try
            {
                using (MemoryStream stream = new MemoryStream(data))
                using (BinaryReader reader = new BinaryReader(stream, Encoding.UTF8))
                {
                    // name length + name
                    byte nameLength = reader.ReadByte();
                    byte[] nameBytes = reader.ReadBytes(nameLength);
                    string packetName = Encoding.UTF8.GetString(nameBytes);

                    // clientId (byte[16] Guid)
                    byte[] clientIdBytes = reader.ReadBytes(16);
                    Guid clientId = new Guid(clientIdBytes);

                    // remaining payload
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
                                t.Name,
                                packetName,
                                StringComparison.Ordinal));

                    if (type == null)
                    {
                        Logger.Error("UnpackPacket", $"Unknown packet type '{packetName}'");
                        return (packetName, clientId, null);
                    }

                    var tmp = (Packet)FormatterServices.GetUninitializedObject(type)!;

                    using (MemoryStream payloadStream = new MemoryStream(payload))
                    using (BinaryReader payloadReader = new BinaryReader(payloadStream, Encoding.UTF8))
                        return (packetName, clientId, tmp.Deserialize(clientId, payloadReader));
                }
            }
            catch (Exception ex)
            {
                Logger.Error("UnpackPacket", $"Failed to unpack: {ex}");
                return (null, null, null);
            }
        }

    }

}
