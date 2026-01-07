using SilkBound.Network;
using System;
using System.Collections.Generic;
using System.Text;

namespace SilkBound.Sync {
    public class ReplicatedHealth {
        private float hp;
        public float Health => hp;

        private NetworkEntity? Entity;
        public void UpdateHost(NetworkEntity entity) => Entity = entity;
        public void TakeDamage(Weaver source, HitInstance hit)
        {
            if (Entity == null)
                return;

            if (Entity.Owner != source) {

                return;
            }
        }
    }
}
