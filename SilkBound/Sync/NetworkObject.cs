using SilkBound.Extensions;
using SilkBound.Managers;
using SilkBound.Network;
using SilkBound.Network.Packets.Impl.Communication;
using SilkBound.Types;
using SilkBound.Utils;
using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace SilkBound.Sync {
    public abstract class NetworkObject : GenericSync {
        public virtual bool Active => NetworkUtils.IsNullPtr(gameObject) || gameObject.activeInHierarchy;

        private Guid? _networkId = null;
        public Guid NetworkId => _networkId ??= BuildIdentifier(gameObject);

        private Weaver? _owner = null;
        public Weaver? Owner {
            get
            {
                return _owner;
            }
        }

        public bool IsLocalOwned => Owner == NetworkUtils.LocalClient;
        public bool IsServerOwned => Owner == Server.CurrentServer.Host || (IsLocalOwned && NetworkUtils.IsServer);

        public void TransferOwnership(Weaver newOwner) {
            Weaver? prev = _owner;
            _owner = newOwner;

            if (NetworkUtils.IsServer)
                NetworkUtils.SendPacket(new AcknowledgeNetworkOwnerPacket(NetworkId, newOwner.ClientID));

            if (prev != null)
                OnOwnershipChanged(prev, newOwner);
            else
                OwnershipInitialized(newOwner);
        }
        protected sealed override void Start() {
            NetworkObjectManager.Register(this);
            if (NetworkUtils.IsServer) {
                TransferOwnership(NetworkUtils.LocalClient);
            }
        }
        protected sealed override void Reset() {
            NetworkObjectManager.Unregister(this);
            Logger.Warn("implement networkobjectdestroyed joe you dumbass you forgot");
            //if (IsLocalOwned)
            //    NetworkUtils.SendPacket(new NetworkObjectDestroyed(NetworkId));
        }

        protected abstract void OnStart();
        protected virtual void OnOwnershipChanged(Weaver prev, Weaver owner) { }
        protected virtual void OwnershipInitialized(Weaver owner) { }
        // TODO: public virtual bool OwnershipAvailable(Weaver requesting) => false;




        public static Guid FromString(string input)
        {
            using var provider = MD5.Create();
            return new Guid(provider.ComputeHash(Encoding.UTF8.GetBytes(input)));
        }
        public static Guid BuildIdentifier(GameObject obj) {
            return FromString(
                new StringBuilder()
                    .Append("NETOBJ|")
                    .Append(obj.tag)
                    .Append('|')
                    .Append(obj.transform
                        .GetPath()
                        .Replace(" ", "+")
                        .Replace("/", "."))
                    .Append(obj.GetComponents<Component>().Length)
                    .Append(obj.layer)
                    .Append(obj.scene.buildIndex)
                .ToString());
        }
    }
}
