using SilkBound.Network;
using SilkBound.Sync;
using SilkBound.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

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
        //public static void Flush(bool flushActive=false)
        //{
        //    if (flushActive)
        //    {
        //        NetworkObjects.Clear();
        //        return;
        //    }

        //    NetworkObjects.RemoveAll(obj => !obj.Active);
        //}
        public static NetworkObject? Get(Guid id)
        {
            return NetworkObjects.Find(o => o.NetworkId == id);
        }
        public static T? GetComponent<T>(Guid id) where T : Component
        {
            return Get(id)?.GetComponent<T>();
        }
        public static T? Get<T>(Guid id) where T : NetworkObject
        {
            return NetworkObjects.Find(o => o.NetworkId == id) as T;
        }

        public static bool TryGet(Guid id, [NotNullWhen(true)] out NetworkObject netObj)
        {
            NetworkObject? found = NetworkObjects.Find(o => o.NetworkId == id);
            netObj = found;
            return found != null;
        }

        public static bool TryGet<T>(Guid id, [NotNullWhen(true)] out T netObj) where T : NetworkObject
        {
            bool found = TryGet(id, out NetworkObject intermediate);
            netObj = (T) intermediate; // considered using `as` here but if the cast fails i want it to throw instead of causing it to throw some random nullref somewhere else
            return found;
        }

        /// <summary>
        /// Only call this when shutting down the connection. Clears all networked objects of their networked properties.
        /// </summary>
        internal static void Reset()
        {
            foreach (var obj in NetworkObjects)
                Object.Destroy(obj);
        }

        public static void RevokeOwnership(Weaver target)
        {
            // all roads lead to rome my friend
            //if(!NetworkUtils.IsServer)
            //    throw new InvalidOperationException("Only the server can manage network ownership.");

            foreach (var obj in NetworkObjects)
                if(obj.Owner == target)
                    obj.TransferOwnership(NetworkUtils.LocalClient);
        }
    }
}
