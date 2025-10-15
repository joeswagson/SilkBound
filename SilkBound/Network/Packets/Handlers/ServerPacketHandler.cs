using GlobalSettings;
using SilkBound.Behaviours;
using SilkBound.Managers;
using SilkBound.Network.Packets.Impl;
using SilkBound.Network.Packets.Impl.Communication;
using SilkBound.Network.Packets.Impl.Mirror;
using SilkBound.Network.Packets.Impl.Sync.Attacks;
using SilkBound.Network.Packets.Impl.Sync.World;
using SilkBound.Network.Packets.Impl.World;
using SilkBound.Types;
using SilkBound.Types.Transfers;
using SilkBound.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using static SilkBound.Patches.Simple.Attacks.ObjectPoolPatches;
using UnityEngine;
using Logger = SilkBound.Utils.Logger;
using HutongGames.PlayMaker.Actions;

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
            if (TransactionManager.Fetch<HandshakePacket>(packet.HandshakeId) is HandshakePacket original)
            {
                if (original.Fulfilled) return; 

                original.Fulfilled = true;
                Logger.Msg("Handshake Fulfilled (Server):", packet.ClientId, packet.ClientName, packet.HandshakeId);
                TransactionManager.Revoke(packet.HandshakeId); // original packet now eligible for garbage collection as we have completed this transaction
            }
            else
            {
                Logger.Msg("Handshake Recieved (Server):", packet.ClientId, packet.ClientName, packet.HandshakeId);
                connection.Send(new HandshakePacket(packet.ClientId, NetworkUtils.LocalClient!.ClientName, packet.HandshakeId, NetworkUtils.LocalClient.ClientID)); // reply with same handshake id so the client can acknowledge handshake completion

                //now that we have the client id, we can create a client object for them
                Weaver client = new Weaver(packet.ClientName, connection, packet.ClientId);
                Server.CurrentServer!.Connections.Add(client);

                NetworkUtils.LocalServer!.SendExcept(new ClientConnectionPacket(client.ClientID, client.ClientName), connection);
            }
        }

        [PacketHandler(typeof(TransferDataPacket))]
        public void OnTransferDataPacket(TransferDataPacket packet, NetworkConnection connection)
        {
            Logger.Msg($"Received TransferSaveDataPacket: TransferId={packet.TransferId}, ChunkIndex={packet.ChunkIndex}, TotalChunks={packet.TotalChunks}, DataLength={packet.Data.Length}");
            Transfer? transfer = TransactionManager.Fetch<Transfer>(packet.TransferId.ToString("N"));
            if (transfer == null) return;

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
                TransactionManager.Promise(packet.TransferId.ToString("N"), data);
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

            //send to all clients except sender
            NetworkUtils.LocalServer!.SendExcept(packet, connection);
        }

        [PacketHandler(typeof(UpdateWeaverPacket))]
        public void OnUpdateWeaverPacket(UpdateWeaverPacket packet, NetworkConnection connection)
        {
            (packet.Sender.Mirror ??= HornetMirror.CreateMirror(packet))?.UpdateMirror(packet);

            NetworkUtils.LocalServer!.SendExcept(packet, connection);
        }

        [PacketHandler(typeof(PlayClipPacket))]
        public void OnPlayClipPacket(PlayClipPacket packet, NetworkConnection connection)
        {
            packet.Sender.Mirror?.PlayClip(packet);
            NetworkUtils.LocalServer!.SendExcept(packet, connection);
        }

        [PacketHandler(typeof(PlaySoundPacket))]
        public void OnPlaySoundPacket(PlaySoundPacket packet, NetworkConnection connection)
        {
            Logger.Msg(packet.Sender.Mirror?.IsInScene ?? false);
            if (packet.Sender.Mirror?.IsInScene ?? false)
                if (!SoundManager.Play(packet))
                    Logger.Warn("Sound failed to play");

            NetworkUtils.LocalServer!.SendExcept(packet, connection);
        }

        [PacketHandler(typeof(PrefabSpawnPacket))]
        public void OnPrefabSpawnPacket(PrefabSpawnPacket packet, NetworkConnection connection)
        {
            if (CachedEffects.GetEffect(packet.PrefabName, out CachedEffects.CachedEffect effect))
            {
                if (effect.Prefab == null)
                {
                    Logger.Warn($"Effect {packet.PrefabName} has null prefab, cannot instantiate");
                    return;
                }

                TransactionManager.Promise<bool>(effect.Prefab, true);
                ObjectPool.Spawn(effect.Prefab as GameObject, packet.Parent, packet.Position, packet.Rotation, packet.Steal);
            }
            NetworkUtils.LocalServer!.SendExcept(packet, connection);
        }

        #region Attacks

        [PacketHandler(typeof(NailSlashPacket))]
        public void OnNailSlashPacket(NailSlashPacket packet, NetworkConnection connection)
        {
            packet.slash.StartSlash();
            NetworkUtils.LocalServer!.SendExcept(packet, connection);
        }

        [PacketHandler(typeof(DownspikePacket))]
        public void OnDownspikePacket(DownspikePacket packet, NetworkConnection connection)
        {
            packet.slash.StartSlash();
            NetworkUtils.LocalServer!.SendExcept(packet, connection);
        }

        [PacketHandler(typeof(SyncHitPacket))]
        public void OnSyncHitPacket(SyncHitPacket packet, NetworkConnection connection)
        {
            Logger.Msg("Sender:", packet.Sender);
            Logger.Msg("Component:", packet.Component?.GetType().FullName);
            Logger.Msg("Recieved SyncHitPacket (Server):", packet.Hit, packet.Responder != null, packet.Component != null, packet.Hit.Source);
            // i could replace false with packet.Responder != null, but this is safer. if the mirror doesnt exist for some reason either its a bug or someone sending packets without sending UpdateWeaverPacket. hence, this is why damaging is disabled with this packet
            if (packet.Sender.Mirror?.IsInScene ?? false)
                packet.Responder?.Hit(packet.Hit);

            NetworkUtils.LocalServer!.SendExcept(packet, connection);
        }

        //[PacketHandler(typeof(DownspikeVFXPacket))]
        //public void OnDownspikeVFXPacket(DownspikeVFXPacket packet, NetworkConnection connection)
        //{
        //    packet.Sender.Mirror?.MirrorController?.HandleCollisionTouching(packet.Collision, packet.Position);
        //    NetworkUtils.LocalServer!.SendExcept(packet, connection);
        //}

        #endregion
    }
}
