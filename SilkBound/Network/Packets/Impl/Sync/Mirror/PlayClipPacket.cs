using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SilkBound.Network.Packets.Impl.Mirror
{
    public class PlayClipPacket(Guid id, string clipName, float clipStartTime, float overrideFps) : Packet
    {
        // deserialization structure
        public PlayClipPacket() : this(default, string.Empty, default, default) { }
        public override string PacketName => "PlayClipPacket";

        // accessors
        public Guid id = id;
        public string clipName = clipName;
        public float clipStartTime = clipStartTime;
        public float overrideFps = overrideFps;

        // serialization
        public override void Serialize(BinaryWriter writer)
        {
            writer.Write(id.ToString("N"));
            writer.Write(clipName);
            writer.Write(clipStartTime);
            writer.Write(overrideFps);
        }
        public override Packet Deserialize(BinaryReader reader)
        {
            string id = reader.ReadString();
            string clipName = reader.ReadString();
            float clipStartTime = reader.ReadSingle();
            float overrideFps = reader.ReadSingle();

            return new PlayClipPacket(Guid.Parse(id), clipName, clipStartTime, overrideFps);
        }
    }
}
