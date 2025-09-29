using SilkBound.Behaviours;
using SilkBound.Managers;
using SilkBound.Network.Packets.Impl;
using SilkBound.Types;
using SilkBound.Types.Transfers;
using SilkBound.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine.SceneManagement;

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
            Logger.Msg("recieved");
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
                NetworkUtils.LocalConnection?.Send(new HandshakePacket() { ClientId = packet.ClientId, ClientName = packet.ClientName, HandshakeId = packet.HandshakeId, HostGUID=NetworkUtils.LocalClient!.ClientID.ToString() }); // reply with same handshake id so the client can acknowledge handshake completion

                //now that we have the client id, we can create a client object for them
                Weaver client = new Weaver(packet.ClientName, connection, Guid.Parse(packet.ClientId));
                Server.CurrentServer!.Connections.Add(client);
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
            var client = Server.CurrentServer!.Connections.Find(c => c.ClientID.ToString("N") == packet.ClientId.ToString("N"));
            if (client != null)
            {
                Skin skin = SkinManager.GetOrDefault(packet.SkinName);
                client.AppliedSkin = skin;
                if(client.Mirror != null)
                    SkinManager.ApplySkin(client.Mirror.MirrorSprite, skin);
            }

            //send to all clients
            NetworkUtils.LocalServer!.SendExcept(packet, connection);
        }

        [PacketHandler(typeof(UpdateWeaverPacket))]
        public void OnUpdateWeaverPacket(UpdateWeaverPacket packet, NetworkConnection connection)
        {
            var client = Server.CurrentServer!.Connections.Find(c => c.ClientID.ToString("N") == packet.id.ToString("N"));
            Logger.Msg("found client? ", client == null, client?.Connection == connection);
            if (client != null)
            {
                if (client.Mirror == null)
                    client.Mirror = HornetMirror.CreateMirror(packet);
                else
                    client.Mirror.UpdateMirror(packet);
            }

            //send to all clients except sender
            NetworkUtils.LocalServer!.SendExcept(packet, connection);
        }

        [PacketHandler(typeof(PlayClipPacket))]
        public void OnPlayClipPacket(PlayClipPacket packet, NetworkConnection connection)
        {
            var client = Server.CurrentServer!.Connections.Find(c => c.ClientID.ToString("N") == packet.id.ToString("N"));
            if (client != null && client.Mirror != null)
            {
                client.Mirror.PlayClip(packet);
            }
            //send to all clients except sender
            NetworkUtils.LocalServer!.SendExcept(packet, connection);
        }
    }
}
