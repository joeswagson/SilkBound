using SilkBound.Packets.Impl;
using SilkBound.Types;
using SilkBound.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace SilkBound.Network.Packets
{
    public class ServerPacketHandler : PacketHandler
    {
        public ServerPacketHandler()
        {
            Subscribe(nameof(HandshakePacket), (packet) => OnHandshakePacket((HandshakePacket)packet));
        }

        public override void Initialize()
        {

        }

        public void OnHandshakePacket(HandshakePacket packet)
        {
            Logger.Msg("Handshake Recieved (Server):", packet.ClientId, packet.HandshakeId);
        }
    }
}
