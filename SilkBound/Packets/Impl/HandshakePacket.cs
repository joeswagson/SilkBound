using SilkBound.Managers;
using SilkBound.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace SilkBound.Packets.Impl
{
    public class HandshakePacket : Packet
    {
        public override string PacketName => "HandshakePacket";

        public string ClientId;
        public string ClientName;
        public string HandshakeId;
        public string? HostGUID;
        public bool Fulfilled = false;

        public HandshakePacket() { 
            ClientId = string.Empty; 
            ClientName = string.Empty; 
            HandshakeId = string.Empty;
        }
        public HandshakePacket(string ClientId, string ClientName)
        {
            this.ClientId = ClientId;
            this.ClientName = ClientName;
            this.HandshakeId = Guid.NewGuid().ToString();

            TransactionManager.Promise(HandshakeId, this);
        }

        public override Packet Deserialize(byte[] data)
        {
            using (MemoryStream stream = new MemoryStream(data))
            using (BinaryReader reader = new BinaryReader(stream, Encoding.UTF8))
            {
                string clientId = reader.ReadString();
                string clientName = reader.ReadString();
                string handshakeId = reader.ReadString();
                string? hostGuid = null;
                if (reader.ReadBoolean())
                    hostGuid = reader.ReadString();

                return new HandshakePacket() { ClientId=clientId, ClientName=clientName, HandshakeId = handshakeId, HostGUID = hostGuid };
            }

        }

        public override byte[] Serialize()
        {
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream, Encoding.UTF8))
            {
                writer.Write(ClientId.Substring(0, Math.Min(100, ClientId.Length)));
                writer.Write(ClientName.Substring(0, Math.Min(100, ClientName.Length)));
                writer.Write(HandshakeId.Substring(0, Math.Min(100, HandshakeId.Length)));
                writer.Write(HostGUID != null);
                if (HostGUID != null)
                    writer.Write(HostGUID.Substring(0, Math.Min(100, HostGUID.Length)));


                return stream.ToArray();
            }
        }
    }
}
