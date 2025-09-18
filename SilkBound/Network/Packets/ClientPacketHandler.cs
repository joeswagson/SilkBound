using MelonLoader;
using SilkBound.Managers;
using SilkBound.Packets.Impl;
using SilkBound.Types;
using SilkBound.Types.NetLayers;
using SilkBound.Utils;
using Steamworks;
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

        }

        public override void Initialize()
        {

        }

        [PacketHandler(typeof(HandshakePacket))]
        public void OnHandshakePacket(HandshakePacket packet, NetworkConnection connection)
        {
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
                Logger.Msg("Handshake Recieved (Client):", packet.ClientId, packet.HandshakeId);
                NetworkUtils.LocalConnection?.Send(new HandshakePacket(packet.ClientId, packet.ClientName) { HandshakeId = packet.HandshakeId }); // reply with same handshake id so the client can acknowledge handshake completion
            }
        }

        [PacketHandler(typeof(SteamKickS2CPacket))]
        public void OnSteamKickS2CPacket(SteamKickS2CPacket packet, NetworkConnection connection)
        {
            if (packet.TargetSteamId == SteamUser.GetSteamID().m_SteamID)
            {
                Logger.Msg("Kicked from server:", packet.Reason);
                NetworkUtils.LocalConnection?.Disconnect();
            }
            else
            {
                SteamNetworking.CloseP2PSessionWithUser(new CSteamID(packet.TargetSteamId!.Value));
            }
        }

        [PacketHandler(typeof(RequestEnterAreaPacket))]
        public void OnRequestEnterAreaPacket(RequestEnterAreaPacket packet, NetworkConnection connection)
        {
            Logger.Msg("Received request to enter area:", packet.GateName);
            MelonCoroutines.Start(
                HeroController.instance.EnterScene(
                    GameManager.instance.FindTransitionPoint(packet.GateName, default, false),
                    0,
                    false,
                    null,
                    false
                )
            );
        }
    }
}
