using SilkBound.Extensions;
using SilkBound.Managers;
using SilkBound.Types.JsonConverters;
using SilkBound.Utils;
using System.IO;
using UnityEngine;
namespace SilkBound.Network.Packets.Impl.Sync.Attacks
{
    public class SyncHitPacket(HitInstance hitInstance, string? objectPath=null) : Packet
    {
        public HitInstance Hit => hitInstance;
        public IHitResponder? Responder => objectPath != null ? ObjectManager.Get(objectPath)?.GameObject?.GetComponent<IHitResponder>() : null;
        public MonoBehaviour? Component => Responder as MonoBehaviour;
        public override Packet Deserialize(BinaryReader reader)
        {
            int size = reader.ReadInt32();
            byte[] data = reader.ReadBytes(size);
            HitInstance hit = ChunkedTransfer.Deserialize<HitInstance>(data, new GameObjectConverter(replaceController: false), new ToolItemConverter());

            string transformPath = reader.ReadString();
            //GameObject? go = UnityObjectExtensions.FindObjectFromFullName(transformPath);
            
            return new SyncHitPacket(hit, transformPath);
        }

        public override void Serialize(BinaryWriter writer)
        {
            byte[] data = ChunkedTransfer.Serialize(hitInstance, new GameObjectConverter(replaceController: true), new ToolItemConverter());

            writer.Write(data.Length);
            writer.Write(data);

            writer.Write(objectPath);
        }
    }
}
