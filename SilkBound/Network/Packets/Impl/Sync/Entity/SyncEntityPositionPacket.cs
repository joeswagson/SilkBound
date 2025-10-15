using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SilkBound.Network.Packets.Impl.Mirror;

namespace SilkBound.Network.Packets.Impl.Sync.Entity
{
    public class SyncEntityPositionPacket : UpdateWeaverPacket
    {
        public Guid EntityId;
        public SyncEntityPositionPacket(Guid entityId, string scene, float posX, float posY, float scaleX, float vX, float vY) : base(scene, posX, posY, scaleX, vX, vY)
        {
            EntityId = entityId;
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.Write(EntityId.ToByteArray());
        }

        public override Packet Deserialize(BinaryReader reader)
        {
            UpdateWeaverPacket basePacket = (UpdateWeaverPacket)base.Deserialize(reader);
            Guid entityId = new Guid(reader.ReadBytes(16));
            return new SyncEntityPositionPacket(entityId, basePacket.Scene, basePacket.PosX, basePacket.PosY, basePacket.ScaleX, basePacket.VelocityX, basePacket.VelocityY);
        }
    }
}
