using HutongGames.PlayMaker.Actions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SilkBound.Network.Packets.Impl
{
    public class CompassPositionPacket : Packet
    {
        public override string PacketName => "CompassPositionPacket";
        public string id;
        public bool active;
        public float posX;
        public float posY;
        public CompassPositionPacket()
        {
            this.id = string.Empty;
        }
        public CompassPositionPacket(string id, bool active, float posX, float posY)
        {
            this.id = id;
            this.active = active;
            this.posX = posX;
            this.posY = posY;
        }
        public override byte[] Serialize()
        {
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream, Encoding.UTF8))
            {
                writer.Write(id);
                writer.Write(active);
                writer.Write(posX);
                writer.Write(posY);
                return stream.ToArray();
            }
        }
        public override Packet Deserialize(byte[] bytes)
        {
            using (MemoryStream stream = new MemoryStream(bytes))
            using (BinaryReader reader = new BinaryReader(stream, Encoding.UTF8))
            {
                string id = reader.ReadString();
                bool active = reader.ReadBoolean();
                float posX = reader.ReadSingle();
                float posY = reader.ReadSingle();

                return new CompassPositionPacket(id, active, posX, posY);
            }
        }
    }
}
