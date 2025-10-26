using System.IO;

namespace SilkBound.Network.Packets.Impl.World
{
    public class CompassPositionPacket : Packet
    {
        public string id;
        public bool active;
        public float posX;
        public float posY;
        public CompassPositionPacket()
        {
            id = string.Empty;
        }
        public CompassPositionPacket(string id, bool active, float posX, float posY)
        {
            this.id = id;
            this.active = active;
            this.posX = posX;
            this.posY = posY;
        }
        public override void Serialize(BinaryWriter writer)
        {
            writer.Write(id);
            writer.Write(active);
            writer.Write(posX);
            writer.Write(posY);
        }
        public override Packet Deserialize(BinaryReader reader)
        {
            string id = reader.ReadString();
            bool active = reader.ReadBoolean();
            float posX = reader.ReadSingle();
            float posY = reader.ReadSingle();

            return new CompassPositionPacket(id, active, posX, posY);
        }
    }
}
