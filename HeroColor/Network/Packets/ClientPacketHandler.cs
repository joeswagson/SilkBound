using MelonLoader;
using SilkBound.Packets;
using SilkBound.Packets.Impl;
using SilkBound.Types;
using SilkBound.Utils;
using System;
using System.Collections.Generic;
using System.Text;

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
        }
    }
}
