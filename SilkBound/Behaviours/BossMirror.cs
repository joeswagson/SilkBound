using SilkBound.Network;
using SilkBound.Network.Packets.Impl.Mirror;
using SilkBound.Network.Packets.Impl.Sync.Entity;
using SilkBound.Sync;
using SilkBound.Types;
using SilkBound.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SilkBound.Behaviours
{
    public class BossMirror : NetworkObject
    {
        public GameObject BossRoot = null!;

        public static BossMirror? Create(UpdateBossPacket packet)
        {
            return null;
        }

        protected override void Reset()
        {

        }

        protected override void Start()
        {
            base.Start();
        }

        protected override void Tick(float dt)
        {

        }
    }
}
