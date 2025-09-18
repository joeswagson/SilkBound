using SilkBound.Packets;
using System;
using System.Collections.Generic;
using System.Text;

namespace SilkBound.Network.Packets
{
    public class LoadGamePacket : Packet
    {
        public override string PacketName => "LoadGamePacket";

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
