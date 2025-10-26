using SilkBound.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SilkBound.Managers {
    public class DisposableGameObject(GameObject reference)
    {
        private WeakReference<GameObject> weakReference = new WeakReference<GameObject>(reference);
        public GameObject? GameObject
        {
            get
            {
                if (weakReference.TryGetTarget(out GameObject gameObject))
                {
                    if (NetworkUtils.IsNullPtr(gameObject))
                        weakReference.SetTarget(null);
                }
                return gameObject;
            }
        }
    }
    public class ObjectManager {
        private static Dictionary<string, DisposableGameObject> Cache = [];
    }
}
