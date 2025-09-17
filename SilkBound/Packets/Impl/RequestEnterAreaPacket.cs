using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SilkBound.Packets.Impl
{
    public class RequestEnterAreaPacket : Packet
    {
        public override string PacketName => "EnterAreaPacket";


        public override Packet Deserialize(byte[] data)
        {
            using (MemoryStream stream = new MemoryStream(data))
            using (BinaryReader reader = new BinaryReader(stream, Encoding.UTF8))
            {
                string clientId = reader.ReadString();
                string handshakeId = reader.ReadString();

                return new HandshakePacket(clientId) { HandshakeId = handshakeId };
            }

        }

        public override byte[] Serialize()
        {
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream, Encoding.UTF8))
            {
                //writer.Write(ClientId.Substring(0, Math.Min(100, ClientId.Length)));
                //writer.Write(HandshakeId.Substring(0, Math.Min(100, HandshakeId.Length)));

                return stream.ToArray();
            }
        }
    }
}
