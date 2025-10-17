using GlobalEnums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SilkBound.Network.Packets.Impl.Mirror
{
    public class UpdateWeaverPacket(string scene, float posX, float posY, float scaleX, float vX, float vY, EnvironmentTypes env) : Packet
    {
        public string Scene => scene;
        public float PosX => posX;
        public float PosY => posY;
        public float ScaleX => scaleX;
        public float VelocityX => vX;
        public float VelocityY => vY;
        public EnvironmentTypes Environment => env;

        public override void Serialize(BinaryWriter writer)
        {
            writer.Write(scene);
            writer.Write(posX);
            writer.Write(posY);
            writer.Write(scaleX);
            writer.Write(vX);
            writer.Write(vY);
            writer.Write((int)env);
        }
        public override Packet Deserialize(BinaryReader reader)
        {
            string scene = reader.ReadString();
            float posX = reader.ReadSingle();
            float posY = reader.ReadSingle();
            float scaleX = reader.ReadSingle();
            float vX = reader.ReadSingle();
            float vY = reader.ReadSingle();
            EnvironmentTypes env = (EnvironmentTypes)reader.ReadInt32();

            return new UpdateWeaverPacket(scene, posX, posY, scaleX, vX, vY, env);
        }
    }
}
