using MelonLoader;
using SilkBound.Managers;
using SilkBound.Packets.Impl;
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

                    Server.CurrentServer = new Server((connection as NetworkServer)!);
                    Server.CurrentServer.Host = new Weaver(packet.ClientName, connection, Guid.Parse(packet.ClientId));

                    Logger.Msg("Handshake Fulfilled (Client):", packet.ClientId, packet.HandshakeId);
                    TransactionManager.Revoke(packet.HandshakeId); // mark the original packet for garbage collection as we have completed this transaction
                }
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

        [PacketHandler(typeof(TransferSaveDataPacket))]
        public void OnTransferSaveDataPacket(TransferSaveDataPacket packet, NetworkConnection connection)
        {
            Logger.Msg($"Received TransferSaveDataPacket: TransferId={packet.TransferId}, ChunkIndex={packet.ChunkIndex}, TotalChunks={packet.TotalChunks}, DataLength={packet.Data.Length}");
            TransferSaveDataPacket.TransferData? data = TransactionManager.Fetch<TransferSaveDataPacket.TransferData>(packet.TransferId.ToString("N"));

            if (data != null)
            {
                data.Chunks[packet.ChunkIndex] = packet.Data;
                Logger.Msg($"Received chunk {packet.ChunkIndex}/{data.TotalChunks} for transfer {packet.TransferId} ({data.Chunks.Count(a => a != null)}/{data.TotalChunks} complete)");
            }
            else
            {
                data = new TransferSaveDataPacket.TransferData
                {
                    Chunks = new byte[packet.TotalChunks][],
                    TotalChunks = packet.TotalChunks
                };
                data.Chunks[packet.ChunkIndex] = packet.Data;
                Logger.Msg($"Received chunk {packet.ChunkIndex}/{data.TotalChunks} for transfer {packet.TransferId} (1/{data.TotalChunks} complete)");
                TransactionManager.Promise(packet.TransferId.ToString("N"), data);
            }

            if (data.Chunks.Count(a => a != null) >= data.TotalChunks && NetworkUtils.LocalClient != null)
            {
                SaveDataTransfer? transfer = ChunkedTransfer.Unpack<SaveDataTransfer>(new List<byte[]>(data.Chunks!));
                if(transfer == null)
                {
                    Logger.Msg("Failed to unpack SaveDataTransfer");
                    return;
                }
                NetworkUtils.LocalClient.SaveGame = transfer.Data;
                TransactionManager.Promise(transfer.HostHash, transfer);
                GameManager.instance.LoadGameFromUI(transfer.HostHash, NetworkUtils.LocalClient.SaveGame);
            }
        }


        [PacketHandler(typeof(LoadGameFromUIPacket))]
        public void OnRequestEnterAreaPacket(LoadGameFromUIPacket packet, NetworkConnection connection)
        {

        }
    }
}
