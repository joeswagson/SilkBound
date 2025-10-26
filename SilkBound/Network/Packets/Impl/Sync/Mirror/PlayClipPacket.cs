using System.IO;

namespace SilkBound.Network.Packets.Impl.Mirror
{
    public class PlayClipPacket(string id, string clipName, float clipStartTime, float overrideFps) : Packet
    {
        // accessors
        public string id = id;
        public string clipName = clipName;
        public float clipStartTime = clipStartTime;
        public float overrideFps = overrideFps;

        // serialization
        public override void Serialize(BinaryWriter writer)
        {
            writer.Write(id);
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

            return new PlayClipPacket(id, clipName, clipStartTime, overrideFps);
        }
    }
}
