using System.IO;

namespace SilkBound.Network.Packets.Impl.Mirror
{
    public class SkinUpdatePacket(string skinName) : Packet
    {
        public string SkinName => skinName;

        public override Packet Deserialize(BinaryReader reader)
        {
            return new SkinUpdatePacket(reader.ReadString());
        }

        public override void Serialize(BinaryWriter writer)
        {
            writer.Write(SkinName);
        }
    }
}
