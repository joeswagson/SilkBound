using SilkBound.Behaviours;
using SilkBound.Managers;
using SilkBound.Network.Packets.Impl.Communication;
using SilkBound.Network.Packets.Impl.Mirror;
using SilkBound.Network.Packets.Impl.Sync.Attacks;
using SilkBound.Network.Packets.Impl.Sync.World;
using SilkBound.Network.Packets.Impl.World;
using SilkBound.Types;
using SilkBound.Types.Transfers;
using SilkBound.Utils;
using System;
using System.Linq;
using UnityEngine;
using SilkBound.Extensions;
using SilkBound.Sync;
using SilkBound.Network.Packets.Impl.Sync.Mirror;
using SilkBound.Network.Packets.Impl.Sync.Entity;
using static SilkBound.Patches.Simple.Attacks.ObjectPoolPatches;
using System.Diagnostics;
using SilkBound.Network.NetworkLayers;

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
                _ = connection.Send(new HandshakePacket(packet.ClientId, NetworkUtils.LocalClient!.ClientName, packet.HandshakeId, NetworkUtils.LocalClient.ClientID)); // reply with same handshake id so the client can acknowledge handshake completion

                //now that we have the client id, we can create a client object for them
                Weaver client = new(packet.ClientName, connection, packet.ClientId);
                Server.CurrentServer.Connections.Add(client);

                var currState = ServerState.GetCurrent();
                var transfer = new ServerInformationTransfer(currState);
                _ = TransferManager.Send(transfer: transfer, connections: [connection]);
                _ = new ClientConnectionPacket(client.ClientID, client.ClientName).SendExcept(connection);
            }
        }

        [PacketHandler(typeof(TransferDataPacket))]
        public void OnTransferDataPacket(TransferDataPacket packet, NetworkConnection connection)
        {
            Logger.Msg($"Received TransferDataPacket: TransferId={packet.TransferId}, ChunkIndex={packet.ChunkIndex}, TotalChunks={packet.TotalChunks}, DataLength={packet.Data.Length}");
            Transfer transfer = TransactionManager.Fetch<Transfer?>(packet.TransferId.ToString("N")) ?? TransactionManager.Promise<Transfer>(packet.TransferId.ToString("N"), Transfer.Create(packet.TransferType));

            Transfer.TransferData? data = transfer.ChunkData;

            if (data != null)
            {
                data.Chunks[packet.ChunkIndex] = packet.Data;
                Logger.Msg($"Received chunk {packet.ChunkIndex + 1}/{data.TotalChunks} for transfer {packet.TransferId} ({(float)data.Chunks.Count(a => a != null) / data.TotalChunks:P} complete)");
            }
            else
            {
                data = new Transfer.TransferData
                {
                    Chunks = new byte[packet.TotalChunks][],
                    TotalChunks = packet.TotalChunks
                };
                data.Chunks[packet.ChunkIndex] = packet.Data;
                Logger.Msg($"Received chunk {packet.ChunkIndex + 1}/{data.TotalChunks} for transfer {packet.TransferId} ({1f / data.TotalChunks:P} complete)");
                transfer.ChunkData = data;
                TransactionManager.Promise(packet.TransferId.ToString("N"), transfer);
            }

            if (data.Chunks.Count(a => a != null) >= data.TotalChunks && NetworkUtils.LocalClient != null)
            {
                transfer.Completed([.. data.Chunks], connection);
            }
        }

        [PacketHandler(typeof(SkinUpdatePacket))]
        public void OnSkinUpdatePacket(SkinUpdatePacket packet, NetworkConnection connection)
        {
            packet.Sender.ChangeSkin(SkinManager.GetOrDefault(packet.SkinName));
            //send to all clients except sender
            NetworkUtils.LocalServer!.SendExcept(packet, connection);
        }

        [PacketHandler(typeof(UpdateWeaverPacket))]
        public void OnUpdateWeaverPacket(UpdateWeaverPacket packet, NetworkConnection connection)
        {
            Guid senderBefore = packet.Sender.ClientID;
            (packet.Sender.Mirror ??= HornetMirror.CreateMirror(packet)!)?.UpdateMirror(packet);

            NetworkUtils.LocalServer!.SendExcept(packet, connection);
            Guid senderAfter = packet.Sender.ClientID;
            if (senderBefore != senderAfter)
                Logger.Warn($"Sender Mismatch: {senderBefore}({Server.CurrentServer.GetWeaver(senderBefore)?.ClientName ?? "unk"}) -> {senderAfter}({Server.CurrentServer.GetWeaver(senderAfter)?.ClientName ?? "unk"})");
            //List<NetworkConnection> inScene = NetworkUtils.Server.Connections
            //    .Where(weaver => weaver.ClientID != packet.Sender.ClientID && weaver.Mirror.Scene == packet.Scene)
            //    .Select(weaver=>weaver.Connection)
            //    .ToList();

            //NetworkUtils.LocalServer.SendIncluding(packet, inScene);
        }

        [PacketHandler(typeof(TransitionGhostPacket))]
        public void OnTransitionGhostPacket(TransitionGhostPacket packet, NetworkConnection connection)
        {
            if (packet.Ghosted)
                packet.Sender.Mirror?.Ghost();
            else
                packet.Sender.Mirror?.EndGhost();
            NetworkUtils.LocalServer!.SendExcept(packet, connection);
        }

        //[PacketHandler(typeof(PlayAttackClipPacket))]
        //public void OnPlayAttackClipPacket(PlayAttackClipPacket packet, NetworkConnection connection)
        //{
        //    Logger.Msg("Attack Sync:", $"Crest: {packet.Crest}", "\\", $"Attack: {packet.Attack}", $"ClipName: {packet.ClipName}", $"ClipStartTime: {packet.ClipStartTime}", $"OverrideFps: {packet.OverrideFps}");
        //    GameObject? go = packet.Sender.Mirror?.Attacks?.transform.Find(packet.Crest)?.Find(packet.Attack)?.gameObject; //UnityObjectExtensions.FindObjectFromFullName(packet.Path);
        //    if (go)
        //        go.GetComponent<tk2dSpriteAnimator>()?.Play(go.GetComponent<tk2dSpriteAnimator>().GetClipByName(packet.ClipName), packet.ClipStartTime, packet.OverrideFps);
        //    NetworkUtils.LocalServer!.SendExcept(packet, connection);
        //}
        [PacketHandler(typeof(PlayClipPacket))]
        public void OnPlayClipPacket(PlayClipPacket packet, NetworkConnection connection)
        {
            packet.Sender.Mirror?.PlayClip(packet);

            packet.Relay(connection);
            //NetworkUtils.LocalServer!.SendExcept(packet, connection);
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

        [PacketHandler(typeof(AirDashVFXPacket))]
        public void OnAirDashVFXPacket(AirDashVFXPacket packet, NetworkConnection connection)
        {
            if (packet.Sender.Mirror?.IsInScene ?? false)
                packet.Sender.Mirror.DoAirDashVFX(packet.GroundDash, packet.AirDash, packet.WallSliding, packet.DashDown, packet.Scale);
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

                //TransactionManager.Promise<bool>(effect.Prefab, true);
                GameObject obj = ObjectPool.Spawn(effect.Prefab as GameObject, packet.Parent, packet.Position, packet.Rotation, packet.Steal);
                //Logger.Msg("Spawned:", obj);
                effect.Spawned?.Invoke(packet, obj);
            }
            NetworkUtils.LocalServer!.SendExcept(packet, connection);
        }

        #region Attacks

        [PacketHandler(typeof(NailSlashPacket))]
        public void OnNailSlashPacket(NailSlashPacket packet, NetworkConnection connection)
        {
            packet.Slash.StartSlash();
            NetworkUtils.LocalServer!.SendExcept(packet, connection);
        }

        [PacketHandler(typeof(DownspikePacket))]
        public void OnDownspikePacket(DownspikePacket packet, NetworkConnection connection)
        {
            if (packet.Cancel)
                packet.Slash.CancelAttack();
            else
                packet.Slash.StartSlash();
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

        [PacketHandler(typeof(UpdateHealthPacket))]
        public void OnUpdateHealthPacket(UpdateHealthPacket packet, NetworkConnection connection)
        {
            Logger.Msg($"Received UpdateHealthPacket (Server): Path={packet.Path}, Health={packet.Health}");
            GameObject? obj = ObjectManager.Get(packet.Path)?.GameObject;
            if (obj)
            {
                HealthManager? healthManager = obj.GetComponent<HealthManager>();
                if (healthManager)
                {
                    healthManager.hp = packet.Health;
                }
                else
                {
                    Logger.Warn($"GameObject at Path={packet.Path} does not have a HealthManager component.");
                }
            }
            else
            {
                Logger.Warn($"No GameObject found at Path={packet.Path}.");
            }
            NetworkUtils.LocalServer!.SendExcept(packet, connection);
        }

        //[PacketHandler(typeof(DownspikeVFXPacket))]
        //public void OnDownspikeVFXPacket(DownspikeVFXPacket packet, NetworkConnection connection)
        //{
        //    packet.Sender.Mirror?.MirrorController?.HandleCollisionTouching(packet.Collision, packet.Position);
        //    NetworkUtils.LocalServer!.SendExcept(packet, connection);
        //}

        #endregion

        #region Network Ownership
        [PacketHandler(typeof(UpdateNetworkOwnerPacket))]
        public void OnUpdateNetworkOwnerPacket(UpdateNetworkOwnerPacket packet, NetworkConnection connection)
        {
            if (NetworkObjectManager.TryGet(packet.NetworkId, out NetworkObject netObj) && netObj.Owner != null)
            {
                connection.Send(new AcknowledgeNetworkOwnerPacket(netObj.NetworkId, netObj.Owner.ClientID));

                if (SilkConstants.Server.REQUIRE_SERVER_NETENT_SYNC)
                    return;
            }

            NetworkUtils.LocalServer?.SendExcept(packet, connection);
        }

        #region Entities
        [PacketHandler(typeof(BossGateSensorPacket))]
        public void OnBossGateSensorPacket(BossGateSensorPacket packet, NetworkConnection connection)
        {
            packet.GateSensor?.UpdateSensor(packet.SensorActivated);
            NetworkUtils.LocalServer?.SendExcept(packet, connection);
        }

        [PacketHandler(typeof(SyncEntityPositionPacket))]
        public void OnSyncEntityPositionPacket(SyncEntityPositionPacket packet, NetworkConnection connection)
        {
            if (NetworkObjectManager.TryGet(packet.EntityId, out NetworkEntity netent))
            {
                netent.UpdatePosition(packet.Position, packet.Velocity, packet.ScaleX);
            }
            else if (SilkConstants.Server.REQUIRE_SERVER_NETENT_SYNC)
                return;

            NetworkUtils.LocalServer?.SendExcept(packet, connection);
        }
        #endregion

        #endregion

    }
}
