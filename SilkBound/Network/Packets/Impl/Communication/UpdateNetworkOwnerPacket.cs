using System.IO;

namespace SilkBound.Network.Packets.Impl.Communication
{
    public class UpdateNetworkOwnerPacket(string networkId) : Packet
    {
        public string NetworkId => networkId;
        public override AuthorityNode SendAuthority => AuthorityNode.Client;
        public override AuthorityNode ReadAuthority => AuthorityNode.Server;
        public override Packet Deserialize(BinaryReader reader)
        {
            string netId = reader.ReadString();
            return new UpdateNetworkOwnerPacket(netId);
        }

        public override void Serialize(BinaryWriter writer)
        {
            writer.Write(networkId);
        }
    }
}
