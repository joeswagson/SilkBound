using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace SilkBound.Patches.Simple.Attacks {
    [HarmonyPatch(typeof(NailSlashRecoil))]
    public class NailSlashRecoilPatches {
        //HitResponse(DamageEnemies.HitResponse response)
    }
}
