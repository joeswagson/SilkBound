using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SilkBound.Packets.Impl
{
    public class RequestEnterAreaPacket : Packet
    {
        public override string PacketName => "EnterAreaPacket";

        public RequestEnterAreaPacket() { GateName = string.Empty; }
        
        public string GateName;
        public RequestEnterAreaPacket(string gateName)
        {
            GateName = gateName;
        }

        public override Packet Deserialize(byte[] data)
        {
            using (MemoryStream stream = new MemoryStream(data))
            using (BinaryReader reader = new BinaryReader(stream, Encoding.UTF8))
            {
                return new RequestEnterAreaPacket();
            }
        }

        public override byte[] Serialize()
        {
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream, Encoding.UTF8))
            {
                return stream.ToArray();
            }
        }
    }
}
