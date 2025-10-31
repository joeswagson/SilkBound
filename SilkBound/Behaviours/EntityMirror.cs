using HutongGames.PlayMaker;
using SilkBound.Managers;
using SilkBound.Network;
using SilkBound.Network.Packets.Impl.Mirror;
using SilkBound.Sync;
using SilkBound.Types.Data;
using SilkBound.Types.Language;
using SilkBound.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace SilkBound.Behaviours {
    public class EntityMirror : NetworkEntity {
        public static List<EntityMirror> _mirrors = [];
        public static EntityMirror[] Mirrors
        {
            get
            {
                _mirrors.RemoveAll(mirror => !mirror.Active);
                return [.. _mirrors];
            }
        }


        public static bool Exists(GameObject component, [NotNullWhen(true)] out EntityMirror mirror)
        {
            mirror = component.GetComponent<EntityMirror>();
            return mirror != null;
        }

        private HealthManager? _healthManager = null;
        public HealthManager HealthManager => _healthManager ??= Root.GetComponent<HealthManager>();

        private tk2dSpriteAnimator? _animator;
        private tk2dSpriteAnimator Animator => _animator ??= Root.GetComponent<tk2dSpriteAnimator>();

        private bool fsmsEnabled = false;
        private PlayMakerFSM? Control => Root.LocateMyFSM("Control");
        private PlayMakerFSM[] Fsms => Root.GetComponents<PlayMakerFSM>();
        public NetworkTarget? _target;
        public NetworkTarget Target => _target ??= new NetworkTarget(this);
        public static EntityMirror? Create(GameObject root)
        {
            if (root.GetComponent<EntityMirror>() is var existing && existing != null)
            {
                if (existing.Active)
                {
                    if (!_mirrors.Contains(existing))
                        _mirrors.Add(existing);

                    return existing;
                }

                if (_mirrors.Contains(existing))
                    _mirrors.Remove(existing);
            }


            EntityMirror mirror = root.AddComponent<EntityMirror>();
            _mirrors.Add(mirror);

            mirror.HealthManager.hp = mirror.HealthManager.initHp * (NetworkUtils.Server.Connections.Count + 1);

            return mirror;
        }

        public void UpdateVariables(bool isOwner)
        {
            _ = isOwner; // while being worked on, silence isOwner unused param

            //foreach (var fsm in fsms) {
            //    var vars = fsm.FsmVariables;
            //    //vars.GetGam
            //}
        }
        public void ToggleFSMs(bool enabled)
        {
            //fsms ??= Root.GetComponents<PlayMakerFSM>();
            Logger.Msg("toggling fsms:", enabled);
            fsmsEnabled = enabled;
            foreach (var fsm in Fsms)
            {
                fsm.enabled = enabled;
            }

        }

        public void EnableFSMs()
        {
            if (fsmsEnabled)
                return;

            ToggleFSMs(true);
        }
        public void DisableFSMs()
        {
            if (!fsmsEnabled)
                return;

            ToggleFSMs(false);
        }
        protected override void PreTick(float dt)
        {
            if (fsmsEnabled && Fsms.Any(fsm => fsm.enabled == false))
                EnableFSMs();
            else if (!fsmsEnabled && Fsms.Any(fsm => fsm.enabled == true))
                DisableFSMs();

            if (IsLocalOwned && Target.Update(out HornetMirror? newTarget))
            {
                Logger.Debug("TARGETTING:", newTarget.Client.ClientName);
                UpdateHero(newTarget);
            }
        }

        public void PlayClip(PlayClipPacket packet)
        {
            using (new StackFlag<None>())
            {
                Animator.Play(Animator.Library.GetClipByName(packet.clipName), packet.clipStartTime, packet.overrideFps);
            }
        }

        protected override void OnStart()
        {
            Logger.Msg("Event:", MethodInfo.GetCurrentMethod().Name, "NetOwner:", Owner?.ClientName, $"({Owner?.ClientID})");
        }
        protected override void OwnershipInitialized(Weaver owner)
        {
            Logger.Msg("Event:", MethodInfo.GetCurrentMethod().Name, "NetOwner:", Owner?.ClientName, $"({Owner?.ClientID})");
        }
        protected override void OnOwnershipChanged(Weaver prev, Weaver owner)
        {
            Logger.Msg("Event:", MethodInfo.GetCurrentMethod().Name, "NetOwner:", Owner?.ClientName, $"({Owner?.ClientID})");
            ToggleFSMs(IsLocalOwned);
            //UpdateVariables(IsLocalOwned);
        }

        private FsmGameObject? _cachedHeroField;
        public FsmGameObject? HeroVariable => _cachedHeroField ??= Control?.FsmVariables.FindFsmGameObject("Hero");
        public void UpdateHero(HornetMirror target)
        {
            HeroVariable?.SafeAssign(target.gameObject);
        }
        public void UpdateHero(Weaver target)
        {
            if (target.Mirror == null)
                return;

            UpdateHero(target.Mirror);
        }
    }
}
