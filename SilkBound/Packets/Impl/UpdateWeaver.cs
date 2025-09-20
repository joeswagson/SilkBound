using System;
using System.Collections.Generic;
using System.Text;

namespace SilkBound.Packets.Impl
{
    public class UpdateWeaverPacket : Packet
    {
        public override string PacketName => "UpdateWeaverPacket";

        public override Packet Deserialize(byte[] data)
        {
            throw new NotImplementedException();
        }

        public override byte[] Serialize()
        {
            throw new NotImplementedException();
        }
    }
}
