using SilkBound.Behaviours;
using SilkBound.Extensions;
using SilkBound.Managers;
using SilkBound.Network.Packets.Impl.Communication;
using SilkBound.Network.Packets.Impl.Mirror;
using SilkBound.Network.Packets.Impl.Steam;
using SilkBound.Network.Packets.Impl.Sync.Attacks;
using SilkBound.Network.Packets.Impl.Sync.Entity;
using SilkBound.Network.Packets.Impl.Sync.Mirror;
using SilkBound.Network.Packets.Impl.Sync.World;
using SilkBound.Network.Packets.Impl.World;
using SilkBound.Sync;
using SilkBound.Types;
using SilkBound.Types.Transfers;
using SilkBound.Utils;
using Steamworks;
using System;
using System.Linq;
using UnityEngine;
using static SilkBound.Patches.Simple.Attacks.ObjectPoolPatches;

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
            packet.Sender?.ChangeSkin(SkinManager.GetOrDefault(packet.SkinName));
        }

        [PacketHandler(typeof(ClientConnectionPacket))]
        public void OnClientConnectionPacket(ClientConnectionPacket packet, NetworkConnection connection)
        {
            Weaver client = new(packet.ClientName, null, packet.ClientId);
            Server.CurrentServer?.Connections.Add(client);
        }

        [PacketHandler(typeof(UpdateWeaverPacket))]
        public void OnUpdateWeaverPacket(UpdateWeaverPacket packet, NetworkConnection connection)
        {
            (packet.Sender.Mirror ??= HornetMirror.CreateMirror(packet)!)?.UpdateMirror(packet);
        }

        [PacketHandler(typeof(TransitionGhostPacket))]
        public void OnTransitionGhostPacket(TransitionGhostPacket packet, NetworkConnection connection)
        {
            if (packet.Ghosted)
                packet.Sender.Mirror?.Ghost();
            else
                packet.Sender.Mirror?.EndGhost();
        }

        //[PacketHandler(typeof(PlayAttackClipPacket))]
        //public void OnPlayAttackClipPacket(PlayAttackClipPacket packet, NetworkConnection connection)
        //{
        //    Logger.Msg("Attack Sync:", packet.Crest, "\\", packet.Attack, packet.ClipName, packet.ClipStartTime, packet.OverrideFps);
        //    GameObject? go = packet.Sender.Mirror?.Attacks?.transform.Find(packet.Crest)?.Find(packet.Attack)?.gameObject; //UnityObjectExtensions.FindObjectFromFullName(packet.Path);
        //    if (go)
        //        go.GetComponent<tk2dSpriteAnimator>()?.Play(go.GetComponent<tk2dSpriteAnimator>().GetClipByName(packet.ClipName), packet.ClipStartTime, packet.OverrideFps);
        //}
        [PacketHandler(typeof(PlayClipPacket))]
        public void OnPlayClipPacket(PlayClipPacket packet, NetworkConnection connection)
        {
            if(packet.id.StartsWith("NETOBJ") && NetworkObjectManager.TryGet(packet.id, out NetworkEntity netent) && netent is EntityMirror mirror)
            {
                Logger.Msg("PLAYCLIP:", packet.id, packet.clipName);
                mirror.PlayClip(packet);
            } else
            {
                packet.Sender.Mirror?.PlayClip(packet);
            }
        }

        [PacketHandler(typeof(PlaySoundPacket))]
        public void OnPlaySoundPacket(PlaySoundPacket packet, NetworkConnection connection)
        {
            if (packet.Sender.Mirror?.IsInScene ?? false)
                if (!SoundManager.Play(packet, packet.Position.MultiplyElements(new Vector3(1, 1, 0)) + new Vector3(0, 0, GameCameras.instance.tk2dCam.transform.position.z)))
                    Logger.Warn("Sound failed to play");
        }

        [PacketHandler(typeof(AirDashVFXPacket))]
        public void OnAirDashVFXPacket(AirDashVFXPacket packet, NetworkConnection connection)
        {
            if (packet.Sender.Mirror?.IsInScene ?? false)
                packet.Sender.Mirror.DoAirDashVFX(packet.GroundDash, packet.AirDash, packet.WallSliding, packet.DashDown, packet.Scale);
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
                GameObject obj = ObjectPool.Spawn(effect.Prefab, packet.Parent, packet.Position, packet.Rotation, packet.Steal);
                //Logger.Msg("Spawned:", obj, effect.Name, effect.Prefab, packet.PrefabName);
                if (obj != null)
                    effect.Spawned?.Invoke(packet, obj);
            }
        }

        #region Attacks

        [PacketHandler(typeof(NailSlashPacket))]
        public void OnNailSlashPacket(NailSlashPacket packet, NetworkConnection connection)
        {
            packet.Slash.StartSlash();
        }

        [PacketHandler(typeof(DownspikePacket))]
        public void OnDownspikePacket(DownspikePacket packet, NetworkConnection connection)
        {
            if (packet.Cancel)
                packet.Slash.CancelAttack();
            else
                packet.Slash.StartSlash();
        }

        [PacketHandler(typeof(SyncHitPacket))]
        public void OnSyncHitPacket(SyncHitPacket packet, NetworkConnection connection)
        {
            if (packet.Sender.Mirror?.IsInScene ?? false)
                packet.Responder?.Hit(packet.Hit);
        }
        [PacketHandler(typeof(UpdateHealthPacket))]
        public void OnUpdateHealthPacket(UpdateHealthPacket packet, NetworkConnection connection)
        {
            Logger.Msg($"Received UpdateHealthPacket (Server): Path={packet.Path}, Health={packet.Health}");
            GameObject? obj = UnityObjectExtensions.FindObjectFromFullName(packet.Path);
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
        }
        //[PacketHandler(typeof(DownspikeVFXPacket))]
        //public void OnDownspikeVFXPacket(DownspikeVFXPacket packet, NetworkConnection connection)
        //{
        //    packet.Sender.Mirror?.MirrorController?.HandleCollisionTouching(packet.Collision, packet.Position);
        //}

        #endregion

        #region Network Ownership
        [PacketHandler(typeof(AcknowledgeNetworkOwnerPacket))]
        public void OnAcknowledgeNetworkOwnerPacket(AcknowledgeNetworkOwnerPacket packet, NetworkConnection connection)
        {
            if (NetworkObjectManager.TryGet(packet.NetworkId, out NetworkObject netObj))
            {
                netObj.TransferOwnership(Server.CurrentServer.GetWeaver(packet.OwnerId)!);
                //Logger.Msg($"NetworkObject {netObj.NetworkId} ownership changed to {Server.CurrentServer.GetWeaver(packet.OwnerId)?.ClientName} ({packet.OwnerId})");
            }
        }

        #region Entities
        [PacketHandler(typeof(BossGateSensorPacket))]
        public void OnBossGateSensorPacket(BossGateSensorPacket packet, NetworkConnection connection)
        {
            packet.GateSensor?.UpdateSensor(packet.SensorActivated);
        }

        [PacketHandler(typeof(StartBattlePacket))]
        public void OnStartBattlePacket(StartBattlePacket packet, NetworkConnection connection)
        {
            if (packet.Sender.Mirror?.Scene == packet.Battle?.gameObject.scene.name)
            {
                packet.Battle?.StartBattle();
            }
        }

        [PacketHandler(typeof(SyncEntityPositionPacket))]
        public void OnSyncEntityPositionPacket(SyncEntityPositionPacket packet, NetworkConnection connection)
        {
            if (NetworkObjectManager.TryGet(packet.EntityId, out NetworkEntity netent))
                netent.UpdatePosition(packet.Position, packet.Velocity, packet.ScaleX);
        }
        #endregion

        #endregion
    }
}
