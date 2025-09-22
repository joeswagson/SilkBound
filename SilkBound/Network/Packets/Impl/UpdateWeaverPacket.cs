using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SilkBound.Network.Packets.Impl
{
    public class UpdateWeaverPacket : Packet
    {
        public override string PacketName => "UpdateWeaverPacket";


        public string id = string.Empty;
        public string scene = string.Empty;
        public float posX;
        public float posY;
        public float scaleX;
        public float vX;
        public float vY;

        public UpdateWeaverPacket() { }
        public UpdateWeaverPacket(string id, string scene, float posX, float posY, float scaleX, float vX, float vY)
        {
            this.id = id;
            this.scene = scene;
            this.posX = posX;
            this.posY = posY;
            this.scaleX = scaleX;
            this.vX = vX;
            this.vY = vY;
        }

        public override byte[] Serialize()
        {
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream, Encoding.UTF8))
            {
                writer.Write(id);
                writer.Write(scene);
                writer.Write(posX);
                writer.Write(posY);
                writer.Write(scaleX);
                writer.Write(vX);
                writer.Write(vY);
                return stream.ToArray();
            }
        }
        public override Packet Deserialize(byte[] bytes)
        {
            using (MemoryStream stream = new MemoryStream(bytes))
            using (BinaryReader reader = new BinaryReader(stream, Encoding.UTF8))
            {
                string id = reader.ReadString();
                string scene = reader.ReadString();
                float posX = reader.ReadSingle();
                float posY = reader.ReadSingle();
                float scaleX = reader.ReadSingle();
                float vX = reader.ReadSingle();
                float vY = reader.ReadSingle();

                return new UpdateWeaverPacket(id, scene, posX, posY, scaleX, vX, vY);
            }
        }
    }
}
