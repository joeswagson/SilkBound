using AsmResolver.PE.File.Headers;
using HutongGames.PlayMaker;
using SilkBound.Extensions;
using SilkBound.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SilkBound.Managers {
    public class DisposableGameObject(string path, GameObject? reference) {
        public bool IsDisposed => weakReference == null;
        public string Path => path;
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
        private static Dictionary<string, DisposableGameObject> Cache = [];

        public static DisposableGameObject Register(GameObject target)
        {
            var path = target.transform.GetPath();
            var disp = new DisposableGameObject(path, target);
            Cache[path] = disp;
            return disp;
        }
        public static void Unregister(DisposableGameObject target)
        {
            Cache.Remove(target.Path);
        }

        public static DisposableGameObject Get(string path)
        {
            if (Cache.Count > ENTRIES_BEFORE_FLUSH)
                Flush();

            return Cache[path];
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
