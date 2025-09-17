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
    public class ClientPacketHandler : PacketHandler
    {
        public ClientPacketHandler()
        {
            Subscribe(nameof(HandshakePacket), (packet) => OnHandshakePacket((HandshakePacket)packet));
        }

        public override void Initialize()
        {

        }
        
        public void OnHandshakePacket(HandshakePacket packet)
        {
            Logger.Msg("Handshake Recieved (Client):", packet.ClientId, packet.HandshakeId);
            if (TransactionManager.Fetch<HandshakePacket>(packet.HandshakeId) is HandshakePacket original)
            {
                if (original.Fulfilled) return;
                else
                {
                    original.Fulfilled = true;

                    //do next step of client initialization here or fire an event etc

                    Logger.Msg("Handshake Fulfilled (Client):", packet.ClientId, packet.HandshakeId);
                    TransactionManager.Revoke(packet.HandshakeId); // mark the original packet for garbage collection as we have completed this transaction
                }
            }
            else
            {
                NetworkUtils.LocalConnection?.Send(new HandshakePacket(packet.ClientId) { HandshakeId = packet.HandshakeId }); // reply with same handshake id so the client can acknowledge handshake completion
            }
        }
    }
}
