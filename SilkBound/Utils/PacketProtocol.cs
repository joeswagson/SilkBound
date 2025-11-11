using Galaxy.Api;
using SilkBound.Network.Packets;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using Unity.Collections;

namespace SilkBound.Utils {
    public class PacketProtocol {
        /// <summary>
        /// Search site for packets (recursive).
        /// </summary>
        static string[] ValidRoots =
        [
            "SilkBound.Network.Packets.Impl"
        ];

        #region Packet-ID Registry
        // simple (deterministic) hash algorithm for a packet array for compatibility comparisons on other clients.
        private static int GenerateHash(IEnumerable<Type> types)
        {
            var i = types.Count();
            int hash = i;

            foreach (Type type in types)
            {
                var j = type.Name.Length >> 2;
                var k = type.Name.Sum(c => (byte) c << 2);
                hash += j + k;
            }

            return hash;
        }
        public static readonly int Fingerprint = GenerateHash(GetPacketTypes());

        private static Dictionary<Type, bool> Compression = [];
        private static Dictionary<Type, ushort> HashCache = [];
        private static Dictionary<ushort, Type> Lookup = [];

        public static Type GetPacketType(ushort packetId) => Lookup[packetId];
        public static ushort HashPacket(Type packetType) => HashCache[packetType];
        public static bool Compressed(Type packetType) => Compression[packetType];

        /// <summary>
        /// Populates the packet id registry.
        /// </summary>
        public static void PopulateRegistry()
        {
            Type[] packetTypes = GetPacketTypes();
            for (int i = 0; i < packetTypes.Length; i++)
            {
                Type t = packetTypes[i];
                ushort id = (ushort) i;
                //Compression[t] = t.
                HashCache[t] = id;
                Lookup[id] = t;
            }
        }
        #endregion

        /// <summary>
        /// Takes an instance of a packet and returns a serialized version in the format <c>[Frame Length - <see cref="int"/>] [Packet ID - <see cref="ushort"/>] [Sender ID -  <see cref="byte"/>[16] (<see cref="Guid"/>)] [Serialized Packet Data - <see cref="byte"/>[] (<see cref="Packet.Serialize(BinaryWriter)"/>)]</c>
        /// </summary>
        /// <param name="packet">Target packet.</param>
        /// <returns>The serialized packet.</returns>
        public static byte[]? PackPacket(Packet packet)
        {
            try
            {
                using MemoryStream ms = new();

                Stream stream = packet.IsGzipped
                    ? new GZipStream(ms, CompressionMode.Compress, leaveOpen: true)
                    : ms;


                //Guid clientId = packet.Sender?.ClientID ?? NetworkUtils.ClientID;

                //Logger.Debug("CLIENT_ID:", hash, clientId);
                //if (clientId == NetworkUtils.ClientID)
                //    Logger.Stacktrace();

                using (stream)
                using (BinaryWriter writer = new(stream, Encoding.UTF8, leaveOpen: true))
                    packet.SerializeInternal(writer);

                if (packet.Cancelled)
                    return null; // packet send cancelled

                //stream.Flush();

                byte[] serialized = ms.ToArray();

                // encode packet name
                //string packetName = packet.GetType().Name;
                //byte[] packetNameEncoded = Encoding.UTF8.GetBytes(packetName);
                //byte nameLength = (byte) packetNameEncoded.Length;

                // encode packet id
                ushort packetId = HashPacket(packet.GetType());
                byte[] idBytes = BitConverter.GetBytes(packetId);

                //Logger.Debug("CLIENT_ID:", hash, clientId);
                byte[] clientIdBytes = (packet.Sender?.ClientID ?? NetworkUtils.ClientID).ToByteArray();

                // payload: [nameLen][name][clientId][payload]
                byte[] inner = new byte[idBytes.Length + clientIdBytes.Length + serialized.Length];
                Array.Copy(idBytes, inner, idBytes.Length);
                Array.Copy(clientIdBytes, 0, inner, idBytes.Length, clientIdBytes.Length);
                Array.Copy(serialized, 0, inner, idBytes.Length + clientIdBytes.Length, serialized.Length);


                // prepend size
                byte[] framed = new byte[4 + inner.Length];
                Array.Copy(BitConverter.GetBytes(inner.Length), 0, framed, 0, sizeof(int));
                Array.Copy(inner, 0, framed, sizeof(int), inner.Length);

                return framed;
            } catch(Exception ex)
            {
                Logger.Error(ex);
                throw ex;
            }
        }


        private static Type[]? CachedPacketTypes;

        /// <summary>
        /// Searches the entire current assembly's types for any deriving from <see cref="Packet"/>.
        /// </summary>
        /// <returns>Every registered packet's <see cref="Type"/>.</returns>
        public static Type[] GetPacketTypes()
        {
            return // nneeds to be neat it needs to be NEAT i nnnednd it neeDS it NENEDEDS to be NEAST AND CLEAN it CANT B E disGORGA RNIZED GDhf kh5ugv m5mkieu5c b6dmgjygke6 5k6ik ,5i7
            CachedPacketTypes ??= [
                .. Assembly.GetExecutingAssembly().GetTypes()
                    .Where(t => t.Namespace != null
                                && ValidRoots.Any(root => t.Namespace.StartsWith(
                                        root,
                                        StringComparison.Ordinal
                                    )
                                )
                                && typeof(Packet).IsAssignableFrom(t))
                    .OrderBy(t => t.FullName, StringComparer.Ordinal)
            ];
        }

        /// <summary>
        /// Finds a registered packet type from its class name.
        /// </summary>
        /// <param name="packetName">Target packet class name.</param>
        /// <returns>The <see cref="Type"/> for the packet.</returns>
        public static Type GetPacketType(string packetName)
        {
            return GetPacketTypes().FirstOrDefault(t => string.Equals(
                    t.Name,
                    packetName,
                    StringComparison.Ordinal
                )
            );
        }

        /// <summary>
        /// Converts a packets raw data and returns the corresponding packet data.
        /// </summary>
        /// <param name="data">The serialized packet</param>
        /// <returns>A <see cref="Tuple"/> with the packets <c>Name</c>, <c>Sender</c> and an instance of the packet.</returns>
        public static (ushort, Guid, Packet?)? UnpackPacket(byte[] data)
        {
            try
            {
                using MemoryStream stream = new(data);
                using BinaryReader reader = new(stream, Encoding.UTF8);
                // name length + name
                //byte nameLength = reader.ReadByte();
                //byte[] nameBytes = reader.ReadBytes(nameLength);
                //string packetName = Encoding.UTF8.GetString(nameBytes);

                // packetId (ushort)
                ushort packetId = reader.ReadUInt16();

                // clientId (byte[16] Guid)
                byte[] clientIdBytes = reader.ReadBytes(16);
                Guid clientId = new(clientIdBytes);
                //Logger.Warn("Read clientid:", clientId);

                // remaining payload
                byte[] payload = reader.ReadBytes((int) (stream.Length - stream.Position));
                var type = GetPacketType(packetId);

                if (type == null)
                {
                    Logger.Error("UnpackPacket", $"Unknown packet '{packetId}'");
                    return (packetId, clientId, null);
                }

                var tmp = (Packet) FormatterServices.GetUninitializedObject(type)!;
                using MemoryStream payloadStream = new(payload);
                BinaryReader payloadReader;
                GZipStream? gzs = null;
                if (tmp.IsGzipped)
                {
                    gzs = new GZipStream(payloadStream, CompressionMode.Decompress);
                    payloadReader = new BinaryReader(gzs, Encoding.UTF8);
                } else
                {
                    payloadReader = new BinaryReader(payloadStream, Encoding.UTF8);
                }

                var packet = tmp.Deserialize(clientId, payloadReader);
                payloadReader.Close();
                return (packetId, clientId, packet);
            } catch (Exception ex)
            {
                Logger.Error("UnpackPacket", $"Failed to unpack: {ex}");
                return null;
            }
        }

    }

}
