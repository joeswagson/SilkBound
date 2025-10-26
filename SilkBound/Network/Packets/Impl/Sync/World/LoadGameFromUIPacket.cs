using SilkBound.Utils;
using System.Collections.Generic;
using System.IO;

namespace SilkBound.Network.Packets.Impl.World
{
    public class LoadGameFromUIPacket(int saveSlot, Dictionary<string, string> saveData) : Packet
    {
        public int SaveSlot = saveSlot;
        public Dictionary<string, string> SaveData = saveData;

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
