using SilkBound.Network.Packets;
using System.IO;

namespace SilkBound.Network.Packets.Impl.Sync.Entity
{
    public class UpdateHealthPacket(string path, int health) : Packet
    {
        public string Path => path;
        public int Health => health;

        public override Packet Deserialize(BinaryReader reader)
        {
            string path = reader.ReadString();
            int health = reader.ReadInt32();

            return new UpdateHealthPacket(path, health);
        }

        public override void Serialize(BinaryWriter writer)
        {
            writer.Write(Path);
            writer.Write(Health);
        }
    }
}
