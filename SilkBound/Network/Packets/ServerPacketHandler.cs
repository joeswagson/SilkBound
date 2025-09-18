using SilkBound.Managers;
using SilkBound.Packets.Impl;
using SilkBound.Types;
using SilkBound.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.SceneManagement;

namespace SilkBound.Network.Packets
{
    public class ServerPacketHandler : PacketHandler
    {
        public ServerPacketHandler()
        {
            Subscribe(nameof(HandshakePacket), (packet, connection) => OnHandshakePacket((HandshakePacket)packet, connection));
        }

        public override void Initialize()
        {

        }

        public void OnHandshakePacket(HandshakePacket packet, NetworkConnection connection)
        {
            if (TransactionManager.Fetch<HandshakePacket>(packet.HandshakeId) is HandshakePacket original)
            {
                if (original.Fulfilled) return;
                else
                {
                    original.Fulfilled = true;
                    Logger.Msg("Handshake Fulfilled (Server):", packet.ClientId, packet.ClientName, packet.HandshakeId);
                    TransactionManager.Revoke(packet.HandshakeId); // mark the original packet for garbage collection as we have completed this transaction
                }
            }
            else
            {
                Logger.Msg("Handshake Recieved (Server):", packet.ClientId, packet.ClientName, packet.HandshakeId);
                NetworkUtils.LocalConnection?.Send(new HandshakePacket(packet.ClientId, packet.ClientName) { HandshakeId = packet.HandshakeId }); // reply with same handshake id so the client can acknowledge handshake completion

                //now that we have the client id, we can create a client object for them
                Weaver client = new Weaver(packet.ClientName, Guid.Parse(packet.ClientId));
                if (Server.CurrentServer == null) return;
                Server.CurrentServer.Connections[client] = connection;
            }
        }
    }
}
