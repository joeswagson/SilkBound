using SilkBound.Behaviours;
using SilkBound.Types.Mirrors;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SilkBound.Patches.Overrides.Impl
{
    public class HeroAnimationControllerOverrides : HeroAnimationController
    {
        public HeroAnimationControllerOverrides()
        {
            GenericOverride<HeroAnimationController>.OverrideClass(GetType());
        }

        private new void Awake()
        {
            bool isMirror = HornetMirror.IsMirror(gameObject, out HornetMirror mirror);

            animator = GetComponent<tk2dSpriteAnimator>();
            meshRenderer = GetComponent<MeshRenderer>();
            heroCtrl = isMirror ? new HeroControllerMirror() : GetComponent<HeroController>();
            audioCtrl = GetComponent<HeroAudioController>();
            cState = heroCtrl.cState;
            clearBackflipSpawnedAudio = delegate ()
            {
                backflipSpawnedAudio = null;
            };
        }
    }
}
