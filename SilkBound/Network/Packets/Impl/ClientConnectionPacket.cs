using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SilkBound.Network.Packets.Impl
{
    public class ClientConnectionPacket : Packet
    {
        public override string PacketName => "ClientConnectionPacket";

        public string ClientId;
        public string ClientName;

        public ClientConnectionPacket()
        {
            ClientId = string.Empty;
            ClientName = string.Empty;
        }
        public ClientConnectionPacket(string ClientId, string ClientName)
        {
            this.ClientId = ClientId;
            this.ClientName = ClientName;
        }

        public override Packet Deserialize(BinaryReader reader)
        {
            string clientId = reader.ReadString();
            string clientName = reader.ReadString();

            return new ClientConnectionPacket() { ClientId = clientId, ClientName = clientName };
        }

        public override void Serialize(BinaryWriter writer)
        {
            writer.Write(ClientId.Substring(0, Math.Min(100, ClientId.Length)));
            writer.Write(ClientName.Substring(0, Math.Min(100, ClientName.Length)));
        }
    }
}
