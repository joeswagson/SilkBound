using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SilkBound.Network.Packets.Impl
{
    public class HornetAnimationPacket : Packet
    {
        public override string PacketName => "HornetAnimationPacket";


        public string id = string.Empty;
        public string collectionGuid = string.Empty;
        public int spriteId;

        public HornetAnimationPacket() { }
        public HornetAnimationPacket(string id, string collectionGuid, int spriteId)
        {
            this.id = id;
            this.collectionGuid = collectionGuid;
            this.spriteId = spriteId;
        }

        public override byte[] Serialize()
        {
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream, Encoding.UTF8))
            {
                writer.Write(id);
                writer.Write(collectionGuid);
                writer.Write(spriteId);

                return stream.ToArray();
            }
        }
        public override Packet Deserialize(byte[] bytes)
        {
            using (MemoryStream stream = new MemoryStream(bytes))
            using (BinaryReader reader = new BinaryReader(stream, Encoding.UTF8))
            {
                string id = reader.ReadString();
                string collectionGuid = reader.ReadString();
                int spriteId = reader.ReadInt32();

                return new HornetAnimationPacket(id, collectionGuid, spriteId);
            }
        }
    }
}
