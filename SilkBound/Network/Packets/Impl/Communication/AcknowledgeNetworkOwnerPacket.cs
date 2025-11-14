using System;
using System.IO;

namespace SilkBound.Network.Packets.Impl.Communication
{
    public class AcknowledgeNetworkOwnerPacket(Guid networkId, Guid ownerId) : Packet
    {
        public Guid NetworkId => networkId;
        public Guid OwnerId => ownerId;
        public override AuthorityNode SendAuthority => AuthorityNode.Server;
        public override AuthorityNode ReadAuthority => AuthorityNode.Client;
        public override Packet Deserialize(BinaryReader reader)
        {
            byte[] networkIdBytes = reader.ReadBytes(16);
            byte[] ownerIdBytes = reader.ReadBytes(16);
            Guid networkId = new(networkIdBytes);
            Guid ownerId = new(ownerIdBytes);
            return new AcknowledgeNetworkOwnerPacket(networkId, ownerId);
        }

        public override void Serialize(BinaryWriter writer)
        {
            writer.Write(networkId.ToByteArray());
            writer.Write(ownerId.ToByteArray());
        }
    }
}
