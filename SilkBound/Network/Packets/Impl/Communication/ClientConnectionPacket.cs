using SilkBound.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SilkBound.Network.Packets.Impl.Communication
{
    public class ClientConnectionPacket(Guid clientId, string clientName) : Packet
    {
        public Guid ClientId => clientId;
        public string ClientName => clientName;

        public override Packet Deserialize(BinaryReader reader)
        {
            Guid clientId = new Guid(reader.ReadBytes(16));
            string clientName = reader.ReadString();

            return new ClientConnectionPacket((clientId), clientName);
        }

        public override void Serialize(BinaryWriter writer)
        {
            writer.Write(clientId.ToByteArray());
            writer.Write(clientName.Substring(0, Math.Min(SilkConstants.MAX_NAME_LENGTH, clientName.Length)));
        }
    }
}
