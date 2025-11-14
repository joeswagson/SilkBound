using System;
using System.IO;

namespace SilkBound.Network.Packets.Impl.Communication
{
    public class UpdateNetworkOwnerPacket(Guid networkId) : Packet
    {
        public Guid NetworkId => networkId;
        public override AuthorityNode SendAuthority => AuthorityNode.Client;
        public override AuthorityNode ReadAuthority => AuthorityNode.Server;
        public override Packet Deserialize(BinaryReader reader)
        {
            return new UpdateNetworkOwnerPacket(Read<Guid>());
        }

        public override void Serialize(BinaryWriter writer)
        {
            Write(networkId);
        }
    }
}
