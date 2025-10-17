using SilkBound.Managers;
using SilkBound.Network;
using SilkBound.Network.Packets.Impl.Communication;
using SilkBound.Types;
using SilkBound.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SilkBound.Sync
{
    public abstract class NetworkObject : GenericSync
    {
        public Guid NetworkId { get; }
        private Weaver _owner = null!;
        public Weaver Owner { get
            {
                return _owner ??= Server.CurrentServer.Host;
            }
        }
        public bool IsLocalOwned => Owner == NetworkUtils.LocalClient;
        public bool IsServerOwned => Owner == Server.CurrentServer.Host;
        public void TransferOwnership(Weaver newOwner)
        {
            _owner = newOwner;

            if(NetworkUtils.IsServer)
                NetworkUtils.SendPacket(new AcknowledgeNetworkOwnerPacket(NetworkId, newOwner.ClientID));
            
            OnOwnershipChanged(newOwner);
        }
        protected override void Start()
        {
            NetworkObjectManager.Register(this);
            NetworkUtils.SendPacket(new UpdateNetworkOwnerPacket(NetworkId));
        }
        public virtual void OnOwnershipChanged(Weaver newOwner) { }
    }
}
