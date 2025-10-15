using HutongGames.PlayMaker;
using SilkBound.Extensions;
using SilkBound.Types.JsonConverters;
using SilkBound.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using Logger = SilkBound.Utils.Logger;

namespace SilkBound.Network.Packets.Impl.Sync.Attacks
{
    public class SyncHitPacket(HitInstance hitInstance, IHitResponder? component=null) : Packet
    {
        public HitInstance Hit => hitInstance;
        public IHitResponder? Responder => component;
        public MonoBehaviour? Component => (MonoBehaviour?) component;
        public override Packet Deserialize(BinaryReader reader)
        {
            int size = reader.ReadInt32();
            byte[] data = reader.ReadBytes(size);
            HitInstance hit = ChunkedTransfer.Deserialize<HitInstance>(data, new GameObjectConverter(replaceController: false), new ToolItemConverter());

            string componentTypeName = reader.ReadString();
            string assembly = reader.ReadString();
            Logger.Msg("Type name (r):", $"{componentTypeName}, {assembly}");
            Type componentType = Type.GetType($"{componentTypeName}, {assembly}")!;

            string transformPath = reader.ReadString();
            GameObject? go = UnityObjectExtensions.FindObjectFromFullName(transformPath);

            return new SyncHitPacket(hit, go?.GetComponent(componentType) as IHitResponder);
        }

        public override void Serialize(BinaryWriter writer)
        {
            byte[] data = ChunkedTransfer.Serialize(hitInstance, new GameObjectConverter(replaceController: true), new ToolItemConverter());

            writer.Write(data.Length);
            writer.Write(data);

            Type componentType = component!.GetType();
            Logger.Msg("Type name (w):", componentType.FullName, componentType.Assembly.GetName().Name);
            writer.Write(componentType.FullName);
            writer.Write(componentType.Assembly.GetName().Name);

            writer.Write(((MonoBehaviour)component).transform.GetPath());
        }
    }
}
