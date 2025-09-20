using SilkBound.Managers;
using SilkBound.Network.Packets.Impl;
using SilkBound.Types;
using SilkBound.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.SceneManagement;

namespace SilkBound.Network.Packets.Handlers
{
    public class ServerPacketHandler : PacketHandler
    {
        public ServerPacketHandler()
        {
            //Subscribe(nameof(HandshakePacket), (packet, connection) => OnHandshakePacket((HandshakePacket)packet, connection));
        }

        public override void Initialize()
        {

        }

        [PacketHandler(typeof(HandshakePacket))]
        public void OnHandshakePacket(HandshakePacket packet, NetworkConnection connection)
        {
            Logger.Msg("recieved");
            if (TransactionManager.Fetch<HandshakePacket>(packet.HandshakeId) is HandshakePacket original)
            {
                if (original.Fulfilled) return; 
                else 
                {
                    original.Fulfilled = true;
                    Logger.Msg("Handshake Fulfilled (Server):", packet.ClientId, packet.ClientName, packet.HandshakeId);
                    TransactionManager.Revoke(packet.HandshakeId); // original packet now eligible for garbage collection as we have completed this transaction
                }
            }
            else
            {
                Logger.Msg("Handshake Recieved (Server):", packet.ClientId, packet.ClientName, packet.HandshakeId);
                NetworkUtils.LocalConnection?.Send(new HandshakePacket() { ClientId = packet.ClientId, ClientName = packet.ClientName, HandshakeId = packet.HandshakeId, HostGUID=NetworkUtils.LocalClient!.ClientID.ToString() }); // reply with same handshake id so the client can acknowledge handshake completion

                //now that we have the client id, we can create a client object for them
                Weaver client = new Weaver(packet.ClientName, connection, Guid.Parse(packet.ClientId));
                Server.CurrentServer!.Connections.Add(client);
            }
        }
    }
}
