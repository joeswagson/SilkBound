using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SilkBound.Network.Packets.Impl.Mirror
{
    public class UpdateWeaverPacket : Packet
    {
        public override string PacketName => "UpdateWeaverPacket";


        public Guid id;
        public string scene = string.Empty;
        public float posX;
        public float posY;
        public float scaleX;
        public float vX;
        public float vY;

        public UpdateWeaverPacket() { }
        public UpdateWeaverPacket(Guid id, string scene, float posX, float posY, float scaleX, float vX, float vY)
        {
            this.id = id;
            this.scene = scene;
            this.posX = posX;
            this.posY = posY;
            this.scaleX = scaleX;
            this.vX = vX;
            this.vY = vY;
        }

        public override void Serialize(BinaryWriter writer)
        {
            writer.Write(id.ToString("N"));
            writer.Write(scene);
            writer.Write(posX);
            writer.Write(posY);
            writer.Write(scaleX);
            writer.Write(vX);
            writer.Write(vY);
        }
        public override Packet Deserialize(BinaryReader reader)
        {
            string id = reader.ReadString();
            string scene = reader.ReadString();
            float posX = reader.ReadSingle();
            float posY = reader.ReadSingle();
            float scaleX = reader.ReadSingle();
            float vX = reader.ReadSingle();
            float vY = reader.ReadSingle();

            return new UpdateWeaverPacket(Guid.Parse(id), scene, posX, posY, scaleX, vX, vY);
        }
    }
}
