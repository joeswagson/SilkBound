using AsmResolver.PE.File.Headers;
using HutongGames.PlayMaker;
using SilkBound.Extensions;
using SilkBound.Sync;
using SilkBound.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace SilkBound.Managers {
    public class DisposableGameObject(string path, GameObject? reference) {
        public bool IsDisposed => weakReference == null;
        public readonly string Path = path;
        public readonly Guid Id = NetworkObject.FromString(path);
        private GameObject? weakReference => reference;
        public GameObject? GameObject
        {
            get
            {
                if (NetworkUtils.IsNullPtr(reference))
                {
                    ObjectManager.Unregister(this);
                    return null;
                }

                return reference;
            }
        }

        public string? SafeScene => GameObject?.scene.name;
    }
    public class ObjectManager {
        private const int ENTRIES_BEFORE_FLUSH = 100;
        internal static Dictionary<Guid, DisposableGameObject> Cache = [];

        public static GameObject GlobalContainer => _sbContainer!;
        private static GameObject? _sbContainer;
        internal static void Initialize()
        {
            _sbContainer = new GameObject("SilkBound");
            _sbContainer.tag = "SILKBOUND";
        }

        public static GameObject Empty(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(GlobalContainer.transform);
            return go;
        }

        public static DisposableGameObject Register(GameObject target)
        {
            var path = target.transform.GetPath();
            var disp = new DisposableGameObject(path, target);
            Cache[disp.Id] = disp;
            return disp;
        }
        public static void Unregister(DisposableGameObject target)
        {
            Cache.Remove(target.Id);
        }
        public static T? GetComponent<T>(Guid? path) where T : Component
        {
            return Get(path)?.GameObject?.GetComponent<T>();
        }

        public static DisposableGameObject? Get(Guid? id)
        {
            if (id == null)
                return null;

            if (Cache.Count > ENTRIES_BEFORE_FLUSH)
                Flush();


            if (Cache.ContainsKey(id.Value))
                return Cache[id.Value];

            return null;
        }
        public static DisposableGameObject? Get(string? path)
        {
            if (path == null)
                return null;

            if (Cache.Count > ENTRIES_BEFORE_FLUSH)
                Flush();

            if(Cache.FirstOrDefault(go => go.Value?.Path == path) is var go && go.Value != null)
                return go.Value;

            if(UnityObjectExtensions.FindObjectFromFullName(path) is var go2 && go2 != null)
                return Register(go2);

            return null;
        }

        /// <summary>
        /// Removes any disposed objects from the cache.
        /// </summary>
        /// <returns>The amount of disposed</returns>
        public static int Flush()
        {
            int result = 0;

            foreach (var obj in Cache.Values)
            {
                if (!obj.IsDisposed)
                    continue;

                Unregister(obj);
                result++;
            }

            return result;
        }
    }
}
