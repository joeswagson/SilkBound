using SilkBound.Network;
using SilkBound.Sync;
using SilkBound.Types;
using SilkBound.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace SilkBound.Managers
{
    public class NetworkObjectManager
    {
        public static readonly List<NetworkObject> NetworkObjects = new List<NetworkObject>();
        
        public static void Register(NetworkObject obj)
        {
            if (!NetworkObjects.Contains(obj))
                NetworkObjects.Add(obj);
        }

        public static void Unregister(NetworkObject obj)
        {
            if (NetworkObjects.Contains(obj))
                NetworkObjects.Remove(obj);
        }

        public static bool TryGet(Guid id, out NetworkObject netObj)
        {
            NetworkObject? found = NetworkObjects.Find(o => o.NetworkId == id);
            netObj = found!;
            return found != null;
        }
        public static bool TryGet<T>(Guid id, out T netObj) where T : NetworkObject
        {
            return TryGet(id, out netObj);
        }

        public static void RevokeOwnership(Weaver target)
        {
            if(!NetworkUtils.IsServer)
                throw new InvalidOperationException("Only the server can manage network ownership.");

            foreach (var obj in NetworkObjects)
                obj.TransferOwnership(NetworkUtils.LocalClient);
        }
    }
}
