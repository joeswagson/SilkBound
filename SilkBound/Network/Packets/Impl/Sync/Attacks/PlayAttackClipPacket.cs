using SilkBound.Network.Packets.Impl.Mirror;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SilkBound.Network.Packets.Impl.Sync.Attacks
{
    public class PlayAttackClipPacket(string path, string clipName, float clipStartTime, float overrideFps) : Packet
    {
        public string Path => path;
        public string ClipName => clipName;
        public float ClipStartTime => clipStartTime;
        public float OverrideFps => overrideFps;

        // serialization
        public override void Serialize(BinaryWriter writer)
        {
            writer.Write(path);
            writer.Write(clipName);
            writer.Write(clipStartTime);
            writer.Write(overrideFps);
        }
        public override Packet Deserialize(BinaryReader reader)
        {
            string path = reader.ReadString();
            string clipName = reader.ReadString();
            float clipStartTime = reader.ReadSingle();
            float overrideFps = reader.ReadSingle();

            return new PlayAttackClipPacket(path, clipName, clipStartTime, overrideFps);
        }
    }
}
