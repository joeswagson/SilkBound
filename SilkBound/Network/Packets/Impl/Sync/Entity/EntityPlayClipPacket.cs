using SilkBound.Behaviours;
using SilkBound.Managers;
using SilkBound.Sync;
using SilkBound.Types;
using System;
using System.IO;

namespace SilkBound.Network.Packets.Impl.Mirror
{
    public class EntityPlayClipPacket(Guid id, string clipName, float clipStartTime, float overrideFps) : Packet
    {
        // accessors
        public Guid id = id;
        public string clipName = clipName;
        public float clipStartTime = clipStartTime;
        public float overrideFps = overrideFps;

        // serialization
        public override void Serialize(BinaryWriter writer)
        {
            Write(id);
            Write(clipName);
            Write(clipStartTime);
            Write(overrideFps);
        }
        public override Packet Deserialize(BinaryReader reader)
        {
            return new EntityPlayClipPacket(
                Read<Guid>(),
                Read<string>(),
                Read<float>(),
                Read<float>()
                );
        }

        public override void ClientHandler(NetworkConnection connection)
        {
            if (NetworkObjectManager.TryGet(id, out EntityMirror netent))
                netent.PlayClip(this);
        }

        public override void ServerHandler(NetworkConnection connection)
        {
            if (NetworkObjectManager.TryGet(id, out EntityMirror netent))
                netent.PlayClip(this);
            Relay(connection);
        }
    }
}
