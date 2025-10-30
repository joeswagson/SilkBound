using SilkBound.Network;
using SilkBound.Sync;
using SilkBound.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SilkBound.Managers
{
    public class NetworkObjectManager
    {
        public static readonly List<NetworkObject> NetworkObjects = [];
        
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
        public static void Flush(bool flushActive=false)
        {
            if (flushActive)
            {
                NetworkObjects.Clear();
                return;
            }

            NetworkObjects.RemoveAll(obj => !obj.Active);
        }
        public static NetworkObject? Get(string id)
        {
            NetworkObjects.RemoveAll(obj => !obj.Active);
            return NetworkObjects.Find(o => o.NetworkId == id);
        }
        public static T? Get<T>(string id) where T : NetworkObject
        {
            NetworkObjects.RemoveAll(obj => !obj.Active);
            return NetworkObjects.Find(o => o.NetworkId == id) as T;
        }

        public static bool TryGet(string id, [NotNullWhen(true)] out NetworkObject netObj)
        {
            NetworkObjects.RemoveAll(obj => !obj.Active);

            NetworkObject? found = NetworkObjects.Find(o => o.NetworkId == id);
            netObj = found;
            return found != null;
        }

        public static bool TryGet<T>(string id, [NotNullWhen(true)] out T netObj) where T : NetworkObject
        {
            bool found = TryGet(id, out NetworkObject intermediate);
            netObj = (T) intermediate; // considered using `as` here but if the cast fails i want it to throw instead of causing it to throw some random nullref somewhere else
            return found;
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
