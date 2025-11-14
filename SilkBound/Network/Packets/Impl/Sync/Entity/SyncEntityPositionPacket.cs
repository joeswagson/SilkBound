using System;
using System.IO;
using UnityEngine;

namespace SilkBound.Network.Packets.Impl.Sync.Entity
{
    public class SyncEntityPositionPacket(Guid entityId, string scene, Vector3 position, Vector2 velocity, float scaleX) : Packet
    {
        public Guid EntityId => entityId;
        public string Scene => scene;
        public Vector3 Position => position;
        public Vector2 Velocity => velocity;
        public float ScaleX => scaleX;

        public override void Serialize(BinaryWriter writer)
        {
            Write(EntityId);
            Write(scene);
            Write(position.x);
            Write(position.y);
            Write(velocity.x);
            Write(velocity.y);
            Write(scaleX);
        }

        public override Packet Deserialize(BinaryReader reader)
        {
            return new SyncEntityPositionPacket(
                Read<Guid>(),
                Read<string>(),
                Read<Vector2>(),
                Read<Vector2>(),
                Read<float>()
            );
        }
    }
}
