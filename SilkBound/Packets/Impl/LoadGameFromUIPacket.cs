using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SilkBound.Packets.Impl
{
    public class LoadGameFromUIPacket : Packet
    {
        public override string PacketName => "LoadGameFromUIPacket";

        public LoadGameFromUIPacket() { SaveSlot = string.Empty; }
        
        public int SaveSlot;
        public LoadGameFromUIPacket(int saveSlot)
        {
            SaveSlot = saveSlot;
        }

        public override Packet Deserialize(byte[] data)
        {
            using (MemoryStream stream = new MemoryStream(data))
            using (BinaryReader reader = new BinaryReader(stream, Encoding.UTF8))
            {
                return new LoadGameFromUIPacket();
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
