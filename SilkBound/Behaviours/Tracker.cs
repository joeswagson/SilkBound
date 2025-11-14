using SilkBound.Managers;
using SilkBound.Types.Language;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SilkBound.Behaviours {
    public class Tracker : MonoBehaviour {
        public Guid? Id => self?.Id;
        private DisposableGameObject? self;

        Tracker()
        {
            if(StackFlag<GameObject>.RaisedWithValue)
                self = ObjectManager.Register(StackFlag<GameObject>.Value!);
        }
        private void OnDestroy()
        {
            if (self != null)
                ObjectManager.Unregister(self);
        }

        public static Tracker Track(GameObject obj)
        {
            using (new StackFlag<GameObject>(obj))
                return obj.AddComponentIfNotPresent<Tracker>();
        }
    }
}
