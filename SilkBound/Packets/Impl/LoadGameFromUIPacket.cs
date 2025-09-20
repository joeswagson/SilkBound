using SilkBound.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SilkBound.Packets.Impl
{
    public class LoadGameFromUIPacket : Packet
    {
        public override string PacketName => "LoadGameFromUIPacket";

        public LoadGameFromUIPacket() { SaveData = new Dictionary<string, string>(); }

        public int SaveSlot;
        public Dictionary<string, string> SaveData;
        public LoadGameFromUIPacket(int saveSlot, Dictionary<string, string> saveData)
        {
            SaveSlot = saveSlot;
            SaveData = saveData;
        }

        public override Packet Deserialize(byte[] data)
        {
            using (MemoryStream stream = new MemoryStream(data))
            using (BinaryReader reader = new BinaryReader(stream, Encoding.UTF8))
            {
                return new LoadGameFromUIPacket(reader.ReadInt32(), Serialization.DeserializeDictionary(reader));
            }
        }

        public override byte[] Serialize()
        {
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream, Encoding.UTF8))
            {
                writer.Write(SaveSlot);
                Serialization.SerializeDictionary(writer, SaveData);
                return stream.ToArray();
            }
        }
    }
}
