using System;
using System.IO;

namespace SilkBound.Network.Packets.Impl.Communication
{
    public class AcknowledgeNetworkOwnerPacket(string networkId, Guid ownerId) : Packet
    {
        public string NetworkId => networkId;
        public Guid OwnerId => ownerId;
        public override AuthorityNode SendAuthority => AuthorityNode.Server;
        public override AuthorityNode ReadAuthority => AuthorityNode.Client;
        public override Packet Deserialize(BinaryReader reader)
        {
            string networkId = reader.ReadString();
            byte[] ownerIdBytes = reader.ReadBytes(16);
            Guid ownerId = new(ownerIdBytes);
            return new AcknowledgeNetworkOwnerPacket(networkId, ownerId);
        }

        public override void Serialize(BinaryWriter writer)
        {
            writer.Write(networkId);
            writer.Write(ownerId.ToByteArray());
        }
    }
}
