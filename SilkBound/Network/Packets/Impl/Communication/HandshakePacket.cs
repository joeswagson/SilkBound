﻿using SilkBound.Managers;
using System;
using System.IO;

namespace SilkBound.Network.Packets.Impl.Communication
{
    public class HandshakePacket(Guid clientId, string clientName, Guid? handshakeId = null, Guid? hostGuid = null) : Packet
    {
        public Guid ClientId => clientId;
        //public AuthToken Token => token;
        private Guid? _handshakeId;
        public Guid HandshakeId {
            get
            {
                return _handshakeId ??= (handshakeId.HasValue ? handshakeId.Value : Guid.NewGuid());
            }
        }
        public Guid HostGUID => hostGuid.HasValue ? hostGuid.Value : Guid.NewGuid();
        public string ClientName => clientName;
        public bool Fulfilled = false;

        public override Packet Deserialize(BinaryReader reader)
        {
            Guid clientId = new(reader.ReadBytes(16));
            string clientName = reader.ReadString();
            //DateTime expiry = new DateTime(reader.ReadInt64());
            //AuthToken token = AuthToken.Deserialize(reader.ReadBytes(16));
            Guid handshakeId = new(reader.ReadBytes(16));
            Guid? hostGuid = null;
            if (reader.ReadBoolean())
                hostGuid = new Guid(reader.ReadBytes(16));

            return new HandshakePacket(clientId, clientName, handshakeId, hostGuid);
        }

        public override void Serialize(BinaryWriter writer)
        {
            TransactionManager.Promise(HandshakeId, this);

            writer.Write(ClientId.ToByteArray());
            writer.Write(ClientName.Substring(0, Math.Min(100, ClientName.Length)));
            writer.Write(HandshakeId.ToByteArray());
            writer.Write(HostGUID != null);
            if (HostGUID != null)
                writer.Write(HostGUID.ToByteArray());
        }
    }
}
