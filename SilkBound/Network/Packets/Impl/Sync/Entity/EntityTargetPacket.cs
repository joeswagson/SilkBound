using SilkBound.Behaviours;
using SilkBound.Managers;
using SilkBound.Types;
using SilkBound.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SilkBound.Network.Packets.Impl.Sync.Entity {
    public class EntityTargetPacket(Guid networkId, Guid targetId) : Packet {
        public EntityMirror? Mirror => NetworkObjectManager.Get<EntityMirror>(networkId);
        public Weaver Target => NetworkUtils.GetWeaver(targetId) ?? NetworkUtils.LocalClient;
        public override Packet Deserialize(BinaryReader reader)
        {
            return new EntityTargetPacket(
                Read<Guid>(), 
                Read<Guid>());
        }

        public override void Serialize(BinaryWriter writer)
        {
            Write(networkId);
            Write(targetId);
        }

        public override void ClientHandler(NetworkConnection connection)
        {
            Mirror?.Target.ApplyTarget(this);
        }

        public override void ServerHandler(NetworkConnection connection)
        {
            Mirror?.Target.ApplyTarget(this);
            Tunnel(connection);
        }
    }
}
