using SilkBound.Managers;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SilkBound.Behaviours {
    public class Tracker : MonoBehaviour {
        private DisposableGameObject? self;
        void Start()
        {
            self = ObjectManager.Register(gameObject);
        }
        private void OnDestroy()
        {
            if (self != null)
                ObjectManager.Unregister(self);
        }

        public static Tracker Track(GameObject obj)
        {
            return obj.AddComponentIfNotPresent<Tracker>();
        }
    }
}
