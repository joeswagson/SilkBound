using System.IO;

namespace SilkBound.Network.Packets.Impl.Communication
{
    public class ClientDisconnectionPacket(string reason) : Packet
    {
        public override Packet Deserialize(BinaryReader reader)
        {
            string reason = reader.ReadString();
            return new ClientDisconnectionPacket(reason);
        }

        public override void Serialize(BinaryWriter writer)
        {
            writer.Write(reason);
        }
    }
}
