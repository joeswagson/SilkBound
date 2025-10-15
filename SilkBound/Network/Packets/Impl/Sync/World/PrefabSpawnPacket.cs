using System.IO;
using UnityEngine;
using SilkBound.Extensions;

namespace SilkBound.Network.Packets.Impl.World
{
    public class PrefabSpawnPacket(string prefabName, Vector3 position, Quaternion rotation, Transform? parent, bool steal=false) : Packet
    {
        // accessors
        public string PrefabName => prefabName;
        public Vector3 Position => position;
        public Quaternion Rotation => rotation;
        public Transform? Parent => parent;
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

            string transformPath = parent ? parent.GetPath() : string.Empty;
            writer.Write(transformPath);

            writer.Write(steal);
        }

        public override Packet Deserialize(BinaryReader reader)
        {
            string prefabName = reader.ReadString();

            Vector3 pos = new Vector3(
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle()
            );

            Quaternion rot = new Quaternion(
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle()
            );

            string transformPath = reader.ReadString();
            Transform? resolvedTransform = null;
            if (!string.IsNullOrEmpty(transformPath))
                resolvedTransform = UnityObjectExtensions.FindObjectFromFullName(transformPath)?.transform;

            bool steal = reader.ReadBoolean();

            return new PrefabSpawnPacket(prefabName, pos, rot, resolvedTransform, steal);
        }
    }
}
