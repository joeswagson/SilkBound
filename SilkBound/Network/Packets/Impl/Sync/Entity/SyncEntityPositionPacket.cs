using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using GlobalEnums;
using SilkBound.Network.Packets.Impl.Mirror;

namespace SilkBound.Network.Packets.Impl.Sync.Entity
{
    public class SyncEntityPositionPacket : UpdateWeaverPacket
    {
        public Guid EntityId;
        public SyncEntityPositionPacket(Guid entityId, string scene, float posX, float posY, float scaleX, float vX, float vY, EnvironmentTypes env) : base(scene, posX, posY, scaleX, vX, vY, env)
        {
            EntityId = entityId;
        }

        public override void Serialize(BinaryWriter writer)
        {
            writer.Write(EntityId.ToByteArray());
            base.Serialize(writer);
        }

        public override Packet Deserialize(BinaryReader reader)
        {
            Guid entityId = new Guid(reader.ReadBytes(16));
            UpdateWeaverPacket basePacket = (UpdateWeaverPacket)base.Deserialize(reader);
            return new SyncEntityPositionPacket(entityId, basePacket.Scene, basePacket.PosX, basePacket.PosY, basePacket.ScaleX, basePacket.VelocityX, basePacket.VelocityY, basePacket.Environment);
        }
    }
}
