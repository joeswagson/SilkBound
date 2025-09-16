using SilkBound.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace SilkBound.Packets.Impl
{
    public class HandshakePacket : Packet
    {
        public override string PacketName => "HandshakePacket";

        public string ClientId;
        public string HandshakeId;

        public HandshakePacket()
        {
        }
        public HandshakePacket(string ClientId, string HandshakeId)
        {
            this.ClientId = ClientId;
            this.HandshakeId = HandshakeId;
        }

        public override Packet Deserialize(byte[] data)
        {
            using (MemoryStream stream = new MemoryStream(data))
            using (BinaryReader reader = new BinaryReader(stream, Encoding.UTF8))
            {
                return new HandshakePacket(reader.ReadString(), reader.ReadString());
            }

        }

        public override byte[] Serialize()
        {
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream, Encoding.UTF8))
            {
                writer.Write(ClientId.Substring(0, Math.Min(100, ClientId.Length)));
                writer.Write(HandshakeId.Substring(0, Math.Min(100, HandshakeId.Length)));

                Logger.Msg("wrote to writer");
                return stream.ToArray();
            }
        }

        public override Packet? Create(params object[] values)
        {
            if (Assertions.EnsureLength(values, 2)) return null;

            return new HandshakePacket(values[0].ToString(), values[1].ToString());
        }
    }
}
