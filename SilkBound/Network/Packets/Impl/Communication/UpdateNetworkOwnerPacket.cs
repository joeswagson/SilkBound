using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SilkBound.Network.Packets.Impl.Communication
{
    public class UpdateNetworkOwnerPacket(Guid networkId) : Packet
    {
        public Guid NetworkId => networkId;
        public override AuthorityNode ReadAuthority => AuthorityNode.Server;
        public override AuthorityNode SendAuthority => AuthorityNode.Client;
        public override Packet Deserialize(BinaryReader reader)
        {
            Guid netId = new Guid(reader.ReadBytes(16));
            return new UpdateNetworkOwnerPacket(netId);
        }

        public override void Serialize(BinaryWriter writer)
        {
            writer.Write(networkId.ToByteArray());
        }
    }
}
