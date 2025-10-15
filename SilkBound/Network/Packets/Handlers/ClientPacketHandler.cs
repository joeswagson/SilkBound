using GlobalSettings;
using MelonLoader;
using Mono.Unix.Native;
using SilkBound.Behaviours;
using SilkBound.Managers;
using SilkBound.Network.Packets.Impl;
using SilkBound.Network.Packets.Impl.Communication;
using SilkBound.Network.Packets.Impl.Mirror;
using SilkBound.Network.Packets.Impl.Steam;
using SilkBound.Network.Packets.Impl.Sync.Attacks;
using SilkBound.Network.Packets.Impl.Sync.World;
using SilkBound.Network.Packets.Impl.World;
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
using static SilkBound.Patches.Simple.Attacks.ObjectPoolPatches;
using static UnityEngine.UI.Image;
using Logger = SilkBound.Utils.Logger;
using Object = UnityEngine.Object;

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

        public event Action? HandshakeFulfilled;

        [PacketHandler(typeof(HandshakePacket))]
        public void OnHandshakePacket(HandshakePacket packet, NetworkConnection connection)
        {
            if (TransactionManager.Fetch<HandshakePacket>(packet.HandshakeId) is HandshakePacket original)
            {
                if (original.Fulfilled) return;

                original.Fulfilled = true;

                //Server.CurrentServer = new Server((connection as NetworkServer)!);
                Server.CurrentServer!.Host = new Weaver(packet.ClientName, connection, packet.HostGUID);
                Server.CurrentServer.Connections.Add(Server.CurrentServer.Host);

                Logger.Msg("Handshake Fulfilled (Client):", packet.ClientId, packet.HandshakeId);
                TransactionManager.Revoke(packet.HandshakeId); // original packet now eligible for garbage collection as we have completed this transaction

                HandshakeFulfilled?.Invoke();
            }
            else
            {
                Logger.Msg("Handshake Recieved (Client):", packet.ClientId, packet.HandshakeId);
                NetworkUtils.LocalConnection?.Send(new HandshakePacket(packet.ClientId, packet.ClientName, packet.HandshakeId)); // reply with same handshake id so the client can acknowledge handshake completion
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
            Skin skin = SkinManager.GetOrDefault(packet.SkinName);
            packet.Sender.AppliedSkin = skin;
            if (packet.Sender.Mirror != null)
                SkinManager.ApplySkin(packet.Sender.Mirror.MirrorSpriteCollection, skin);
        }

        [PacketHandler(typeof(ClientConnectionPacket))]
        public void OnClientConnectionPacket(ClientConnectionPacket packet, NetworkConnection connection)
        {
            Weaver client = new Weaver(packet.ClientName, null, packet.ClientId);
            Server.CurrentServer?.Connections.Add(client);
        }

        [PacketHandler(typeof(UpdateWeaverPacket))]
        public void OnUpdateWeaverPacket(UpdateWeaverPacket packet, NetworkConnection connection)
        {
            (packet.Sender.Mirror ??= HornetMirror.CreateMirror(packet))?.UpdateMirror(packet);
        }
        [PacketHandler(typeof(PlayClipPacket))]
        public void OnPlayClipPacket(PlayClipPacket packet, NetworkConnection connection)
        {
            var client = Server.CurrentServer!.Connections.Find(c => c.ClientID.ToString("N") == packet.id.ToString("N"));
            if (client != null && client.Mirror != null)
            {
                client.Mirror.PlayClip(packet);
            }
        }

        [PacketHandler(typeof(PlaySoundPacket))]
        public void OnPlaySoundPacket(PlaySoundPacket packet, NetworkConnection connection)
        {
            if (packet.Sender.Mirror?.IsInScene ?? false)
                if (!SoundManager.Play(packet, packet.Position.MultiplyElements(new Vector3(1, 1, 0)) + new Vector3(0, 0, GameCameras.instance.tk2dCam.transform.position.z)))
                    Logger.Warn("Sound failed to play");
        }

        [PacketHandler(typeof(PrefabSpawnPacket))]
        public void OnPrefabSpawnPacket(PrefabSpawnPacket packet, NetworkConnection connection)
        {
            if (CachedEffects.GetEffect(packet.PrefabName, out CachedEffects.CachedEffect effect))
            {
                if(effect.Prefab == null)
                {
                    Logger.Warn($"Effect {packet.PrefabName} has null prefab, cannot instantiate");
                    return;
                }


                Logger.Msg("SPAWNMING!!!!!");
                TransactionManager.Promise<bool>(effect.Prefab, true);
                ObjectPool.Spawn(effect.Prefab as GameObject, packet.Parent, packet.Position, packet.Rotation, packet.Steal);
            }
        }

        #region Attacks

        [PacketHandler(typeof(NailSlashPacket))]
        public void OnNailSlashPacket(NailSlashPacket packet, NetworkConnection connection)
        {
            packet.slash.StartSlash();
        }

        [PacketHandler(typeof(DownspikePacket))]
        public void OnDownspikePacket(DownspikePacket packet, NetworkConnection connection)
        {
            packet.slash.StartSlash();
        }

        [PacketHandler(typeof(SyncHitPacket))]
        public void OnSyncHitPacket(SyncHitPacket packet, NetworkConnection connection)
        {
            if (packet.Sender.Mirror?.IsInScene ?? false)
                packet.Responder?.Hit(packet.Hit);
        }
        //[PacketHandler(typeof(DownspikeVFXPacket))]
        //public void OnDownspikeVFXPacket(DownspikeVFXPacket packet, NetworkConnection connection)
        //{
        //    packet.Sender.Mirror?.MirrorController?.HandleCollisionTouching(packet.Collision, packet.Position);
        //}

        #endregion
    }
}
