using System.IO;
using UnityEngine;

namespace SilkBound.Network.Packets.Impl.Sync.Entity
{
    public class SyncEntityPositionPacket(string entityId, string scene, Vector3 position, Vector2 velocity, float scaleX) : Packet
    {
        public string EntityId => entityId;
        public string Scene => scene;
        public Vector3 Position => position;
        public Vector2 Velocity => velocity;
        public float ScaleX => scaleX;

        public override void Serialize(BinaryWriter writer)
        {
            writer.Write(EntityId);
            writer.Write(scene);
            writer.Write(position.x);
            writer.Write(position.y);
            writer.Write(position.z);
            writer.Write(velocity.x);
            writer.Write(velocity.y);
            writer.Write(scaleX);
        }

        public override Packet Deserialize(BinaryReader reader)
        {
            string entityId = reader.ReadString();
            string scene = reader.ReadString();
            Vector3 position = new(
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle()
            );
            Vector2 velocity = new(
                reader.ReadSingle(),
                reader.ReadSingle()
            );
            float scaleX = reader.ReadSingle();
            return new SyncEntityPositionPacket(entityId, scene, position, velocity, scaleX);
        }
    }
}
