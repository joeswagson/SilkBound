using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace SilkBound.Network.Packets.Impl.Sync.World
{
    public class PlaySoundPacket(string soundName, Vector3 position, float mindist, float maxdist, float volume) : Packet
    {
        public string SoundName => soundName;
        public Vector3 Position => position;
        public float MinDistance => mindist;
        public float MaxDistance => maxdist;
        public float Volume => volume;

        public override Packet Deserialize(BinaryReader reader)
        {
            // youll never see these comments again i just thought it looked cool lol
            return new PlaySoundPacket(
                reader.ReadString(),     // soundName: string
                new Vector3(
                    reader.ReadSingle(), // x: float
                    reader.ReadSingle(), // y: float
                    reader.ReadSingle()  // z: float
                ),
                reader.ReadSingle(),     // mindist: float
                reader.ReadSingle(),     // maxdist: float
                reader.ReadSingle()      // volume: float
            );
        }

        public override void Serialize(BinaryWriter writer)
        {
            writer.Write(soundName);
            writer.Write(position.x);
            writer.Write(position.y);
            writer.Write(position.z);
            writer.Write(mindist);
            writer.Write(maxdist);
            writer.Write(volume);
        }
    }
}
