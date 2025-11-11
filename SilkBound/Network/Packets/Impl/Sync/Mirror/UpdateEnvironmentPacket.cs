using GlobalEnums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SilkBound.Network.Packets.Impl.Sync.Mirror {
    public class UpdateEnvironmentPacket(EnvironmentTypes type) : Packet {
        public EnvironmentTypes Environment => type;
        public override Packet Deserialize(BinaryReader reader)
        {
            return new UpdateEnvironmentPacket((EnvironmentTypes) reader.ReadInt32());
        }

        public override void Serialize(BinaryWriter writer)
        {
            writer.Write((int) type);
        }
    }
}
