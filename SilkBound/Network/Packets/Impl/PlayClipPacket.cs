using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SilkBound.Network.Packets.Impl
{
    public class PlayClipPacket(Guid id, string clipName, float speedMultiplier) : Packet
    {
        // deserialization structure
        public PlayClipPacket() : this(default, string.Empty, default) { }
        public override string PacketName => "PlayClipPacket";

        // accessors
        public Guid id = id;
        public string clipName = clipName;
        public float speedMultiplier = speedMultiplier;

        // serialization
        public override void Serialize(BinaryWriter writer)
        {
            writer.Write(id.ToString("N"));
            writer.Write(clipName);
            writer.Write(speedMultiplier);
        }
        public override Packet Deserialize(BinaryReader reader)
        {
            string id = reader.ReadString();
            string clipName = reader.ReadString();
            float speedMultiplier = reader.ReadSingle();

            return new PlayClipPacket(Guid.Parse(id), clipName, speedMultiplier);
        }
    }
}
