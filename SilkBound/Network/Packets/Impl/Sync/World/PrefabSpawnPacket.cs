using System.IO;
using UnityEngine;
using SilkBound.Extensions;
using SilkBound.Managers;

namespace SilkBound.Network.Packets.Impl.World
{
    public class PrefabSpawnPacket(string prefabName, Vector3 position, Quaternion rotation, string? transformPath, bool steal=false) : Packet
    {
        // accessors
        public string PrefabName => prefabName;
        public Vector3 Position => position;
        public Quaternion Rotation => rotation;
        public Transform? Parent => ObjectManager.Get(transformPath)?.GameObject?.transform;
        public bool Steal => steal;

        // serialization
        public override void Serialize(BinaryWriter writer)
        {
            writer.Write(prefabName);

            writer.Write(position.x);
            writer.Write(position.y);
            writer.Write(position.z);

            writer.Write(rotation.x);
            writer.Write(rotation.y);
            writer.Write(rotation.z);
            writer.Write(rotation.w);

            writer.Write((transformPath ?? string.Empty).ReplaceController());

            writer.Write(steal);
        }

        public override Packet Deserialize(BinaryReader reader)
        {
            string prefabName = reader.ReadString();

            Vector3 pos = new(
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle()
            );

            Quaternion rot = new(
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle()
            );

            string transformPath = reader.ReadString();
            bool steal = reader.ReadBoolean();

            return new PrefabSpawnPacket(prefabName, pos, rot, transformPath, steal);
        }
    }
}
