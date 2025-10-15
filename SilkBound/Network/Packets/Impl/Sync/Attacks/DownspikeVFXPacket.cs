//using Newtonsoft.Json;
//using SilkBound.Types.JsonConverters;
//using SilkBound.Utils;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Text;
//using UnityEngine;

//namespace SilkBound.Network.Packets.Impl.Sync.Attacks
//{
//    public class DownspikeVFXPacket(Collision2D collision, Vector3? downspikeEffectPrefabSpawnPoint) : Packet
//    {
//        Collision2DConverter Collision2DConverterLocal => new Collision2DConverter(true, NetworkUtils.LocalClient);
//        static Collision2DConverter Collision2DConverterRecieve => new Collision2DConverter(false);

//        public Collision2D Collision => collision;
//        public Vector3? Position => downspikeEffectPrefabSpawnPoint;
//        public override Packet Deserialize(BinaryReader reader)
//        {
//            string collisionSerialized = reader.ReadString();
//            Vector3? position = null;
//            if (reader.ReadBoolean())
//                position = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
//            return new DownspikeVFXPacket(JsonConvert.DeserializeObject<Collision2D>(collisionSerialized, Collision2DConverterRecieve)!, position);
//        }

//        public override void Serialize(BinaryWriter writer)
//        {
//            bool hasPrefab = downspikeEffectPrefabSpawnPoint.HasValue;

//            writer.Write(JsonConvert.SerializeObject(collision, Collision2DConverterLocal));
//            writer.Write(hasPrefab);
//            if (hasPrefab)
//            {
//                writer.Write(downspikeEffectPrefabSpawnPoint!.Value.x);
//                writer.Write(downspikeEffectPrefabSpawnPoint!.Value.y);
//                writer.Write(downspikeEffectPrefabSpawnPoint!.Value.z);
//            }
//        }
//    }
//}
