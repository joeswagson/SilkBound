using MelonLoader;
using SilkBound.Behaviours;
using SilkBound.Managers;
using SilkBound.Network.Packets.Impl;
using SilkBound.Types;
using SilkBound.Types.NetLayers;
using SilkBound.Types.Transfers;
using SilkBound.Utils;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Burst.Intrinsics;
using UnityEngine;
using UnityEngine.SceneManagement;
using Logger = SilkBound.Utils.Logger;

namespace SilkBound.Network.Packets.Handlers
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

                original.Fulfilled = true;

                Server.CurrentServer = new Server((connection as NetworkServer)!);
                Server.CurrentServer.Host = new Weaver(packet.ClientName, connection, Guid.Parse(packet.ClientId));
                Server.CurrentServer.Connections.Add(Server.CurrentServer.Host);

                Logger.Msg("Handshake Fulfilled (Client):", packet.ClientId, packet.HandshakeId);
                TransactionManager.Revoke(packet.HandshakeId); // original packet now eligible for garbage collection as we have completed this transaction
            }
            else
            {
                Logger.Msg("Handshake Recieved (Client):", packet.ClientId, packet.HandshakeId);
                NetworkUtils.LocalConnection?.Send(new HandshakePacket() { ClientId = packet.ClientId, ClientName=packet.ClientName, HandshakeId = packet.HandshakeId }); // reply with same handshake id so the client can acknowledge handshake completion
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

        [PacketHandler(typeof(TransferDataPacket))]
        public void OnTransferDataPacket(TransferDataPacket packet, NetworkConnection connection)
        {
            Logger.Msg($"Received TransferSaveDataPacket: TransferId={packet.TransferId}, ChunkIndex={packet.ChunkIndex}, TotalChunks={packet.TotalChunks}, DataLength={packet.Data.Length}");
            Transfer transfer = TransactionManager.Fetch<Transfer?>(packet.TransferId.ToString("N")) ?? TransactionManager.Promise<Transfer>(packet.TransferId.ToString("N"), Transfer.Create(packet.TransferType));

            Transfer.TransferData? data = transfer.ChunkData;

            if (data != null)
            {
                data.Chunks[packet.ChunkIndex] = packet.Data;
                Logger.Msg($"Received chunk {packet.ChunkIndex}/{data.TotalChunks} for transfer {packet.TransferId} ({data.Chunks.Count(a => a != null)}/{data.TotalChunks} complete)");
            }
            else
            {
                data = new Transfer.TransferData
                {
                    Chunks = new byte[packet.TotalChunks][],
                    TotalChunks = packet.TotalChunks
                };
                data.Chunks[packet.ChunkIndex] = packet.Data;
                Logger.Msg($"Received chunk {packet.ChunkIndex}/{data.TotalChunks} for transfer {packet.TransferId} (1/{data.TotalChunks} complete)");
                transfer.ChunkData = data;
                TransactionManager.Promise(packet.TransferId.ToString("N"), transfer);
            }

            if (data.Chunks.Count(a => a != null) >= data.TotalChunks && NetworkUtils.LocalClient != null)
            {
                transfer.Completed(new List<byte[]>(data.Chunks!));
            }
        }

        [PacketHandler(typeof(SkinUpdatePacket))]
        public void OnSkinUpdatePacket(SkinUpdatePacket packet, NetworkConnection connection)
        {
            var client = Server.CurrentServer!.Connections.Find(c => c.ClientID.ToString("N") == packet.ClientId.ToString("N"));
            if (client != null)
            {
                Skin skin = SkinManager.GetOrDefault(packet.SkinName);
                client.AppliedSkin = skin;
                if (client.Mirror != null)
                    SkinManager.ApplySkin(client.Mirror.MirrorSprite, skin);
            }
        }

        [PacketHandler(typeof(ClientConnectionPacket))]
        public void OnClientConnectionPacket(ClientConnectionPacket packet, NetworkConnection connection)
        {
            Weaver client = new Weaver(packet.ClientName, null, Guid.Parse(packet.ClientId));
            Server.CurrentServer?.Connections.Add(client);
        }

        [PacketHandler(typeof(UpdateWeaverPacket))]
        public void OnUpdateWeaverPacket(UpdateWeaverPacket packet, NetworkConnection connection)
        {
            var client = Server.CurrentServer!.Connections.Find(c => c.ClientID.ToString("N") == packet.id.ToString("N"));
            Logger.Msg("found client? ", client == null);
            if (client != null)
            {
                if (client.Mirror == null)
                    client.Mirror = HornetMirror.CreateMirror(packet);
                else
                    client.Mirror.UpdateMirror(packet);
            }
        }
    }
}
