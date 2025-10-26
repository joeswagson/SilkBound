using System.IO;

namespace SilkBound.Network.Packets.Impl.Sync.Mirror
{
    public class TransitionGhostPacket(bool ghosted) : Packet
    {
        public bool Ghosted = ghosted;

        public override Packet Deserialize(BinaryReader reader)
        {
            return new TransitionGhostPacket(reader.ReadBoolean());
        }

        public override void Serialize(BinaryWriter writer)
        {
            writer.Write(Ghosted);
        }
    }
}
