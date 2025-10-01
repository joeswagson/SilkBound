using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SilkBound.Network.Packets.Impl.Mirror
{
    public class SkinUpdatePacket : Packet
    {
        public override string PacketName => "SkinUpdatePacket";

        public Guid ClientId;
        public string SkinName = string.Empty;

        public SkinUpdatePacket() { }
        public SkinUpdatePacket(Guid clientId, string skinName)
        {
            ClientId = clientId;
            SkinName = skinName;
        }

        public override Packet Deserialize(BinaryReader reader)
        {
            return new SkinUpdatePacket(Guid.Parse(reader.ReadString()), reader.ReadString());
        }

        public override void Serialize(BinaryWriter writer)
        {
            writer.Write(ClientId.ToString("N"));
            writer.Write(SkinName);
        }
    }
}
