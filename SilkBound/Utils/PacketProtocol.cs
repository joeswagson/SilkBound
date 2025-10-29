using SilkBound.Network.Packets;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

namespace SilkBound.Utils
{
    public class PacketProtocol
    {
        public static byte[]? PackPacket(Packet packet, Guid? sender = null)
        {
            using MemoryStream ms = new();
            using BinaryWriter writer = new(ms, Encoding.UTF8);
            
            //Guid clientId = packet.Sender?.ClientID ?? NetworkUtils.ClientID;
            int hash = packet.GetHashCode();
            
            //Logger.Debug("CLIENT_ID:", hash, clientId);
            //if (clientId == NetworkUtils.ClientID)
            //    Logger.Stacktrace();

            packet.SerializeInternal(writer);
            byte[] serialized = ms.ToArray();
            
            // encode packet name
            string packetName = packet.GetType().Name;
            byte[] packetNameEncoded = Encoding.UTF8.GetBytes(packetName);
            
            byte nameLength = (byte)packetNameEncoded.Length;
            
            //Logger.Debug("CLIENT_ID:", hash, clientId);
            byte[] clientIdBytes = (packet.Sender?.ClientID ?? NetworkUtils.ClientID).ToByteArray();

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

        static readonly string[] validRoots =
        [
            "SilkBound.Network.Packets.Impl"
        ];

        private static IEnumerable<Type>? CachedPacketTypes;

        public static Type[] GetPacketTypes()
        {
            return // nneeds to be neat it needs to be NEAT i nnnednd it neeDS it NENEDEDS to be NEAST AND CLEAN it CANT B E disGORGA RNIZED GDhf kh5ugv m5mkieu5c b6dmgjygke6 5k6ik ,5i7
            [
                .. (CachedPacketTypes ??= Assembly.GetExecutingAssembly().GetTypes()
                    .Where(t => t.Namespace != null
                                && validRoots.Any(root => t.Namespace.StartsWith(
                                        root,
                                        StringComparison.Ordinal
                                    )
                                )
                                && typeof(Packet).IsAssignableFrom(t)
                    )
                )
            ];
        }

        public static Type GetPacketType(string packetName)
        {
            return GetPacketTypes().FirstOrDefault(t => string.Equals(
                    t.Name,
                    packetName,
                    StringComparison.Ordinal
                )
            );
        }

        public static (string?, Guid?, Packet?) UnpackPacket(byte[] data)
        {
            try
            {
                using MemoryStream stream = new(data);
                using BinaryReader reader = new(stream, Encoding.UTF8);
                // name length + name
                byte nameLength = reader.ReadByte();
                byte[] nameBytes = reader.ReadBytes(nameLength);
                string packetName = Encoding.UTF8.GetString(nameBytes);

                // clientId (byte[16] Guid)
                byte[] clientIdBytes = reader.ReadBytes(16);
                Guid clientId = new(clientIdBytes);
                //Logger.Warn("Read clientid:", clientId);

                // remaining payload
                byte[] payload = reader.ReadBytes((int)(stream.Length - stream.Position));

                var type = GetPacketType(packetName);

                if (type == null)
                {
                    Logger.Error("UnpackPacket", $"Unknown packet type '{packetName}'");
                    return (packetName, clientId, null);
                }

                var tmp = (Packet)FormatterServices.GetUninitializedObject(type)!;

                using MemoryStream payloadStream = new(payload);
                using BinaryReader payloadReader = new(payloadStream, Encoding.UTF8);
                return (packetName, clientId, tmp.Deserialize(clientId, payloadReader));
            }
            catch (Exception ex)
            {
                Logger.Error("UnpackPacket", $"Failed to unpack: {ex}");
                return (null, null, null);
            }
        }

    }

}
