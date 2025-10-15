using SilkBound.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SilkBound.Network.Packets.Impl.World
{
    public class LoadGameFromUIPacket : Packet
    {
        public int SaveSlot;
        public Dictionary<string, string> SaveData;
        public LoadGameFromUIPacket(int saveSlot, Dictionary<string, string> saveData)
        {
            SaveSlot = saveSlot;
            SaveData = saveData;
        }

        public override Packet Deserialize(BinaryReader reader)
        {
            return new LoadGameFromUIPacket(reader.ReadInt32(), Serialization.DeserializeDictionary(reader));
        }

        public override void Serialize(BinaryWriter writer)
        {
            writer.Write(SaveSlot);
            Serialization.SerializeDictionary(writer, SaveData);
        }
    }
}
