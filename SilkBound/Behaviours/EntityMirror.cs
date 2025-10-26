using HutongGames.PlayMaker;
using SilkBound.Network;
using SilkBound.Network.Packets.Impl.Mirror;
using SilkBound.Sync;
using SilkBound.Types.Language;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace SilkBound.Behaviours {
    public class EntityMirror : NetworkEntity {
        public static List<EntityMirror> _mirrors = [];
        public static EntityMirror[] Mirrors {
            get
            {
                _mirrors.RemoveAll(mirror => !mirror.Active);
                return [.. _mirrors];
            }
        }


        private HealthManager? _healthManager = null;
        public HealthManager HealthManager => _healthManager ??= Root.GetComponent<HealthManager>();

        private tk2dSpriteAnimator? _animator;
        private tk2dSpriteAnimator Animator => _animator ??= Root.GetComponent<tk2dSpriteAnimator>();

        private bool fsmsEnabled = false;
        private PlayMakerFSM[] Fsms => Root.GetComponents<PlayMakerFSM>();
        public static EntityMirror? Create(GameObject root) {
            if (root.GetComponent<EntityMirror>() is var existing && existing != null) {
                if (existing.Active) {
                    if (!_mirrors.Contains(existing))
                        _mirrors.Add(existing);

                    return existing;
                }

                if (_mirrors.Contains(existing))
                    _mirrors.Remove(existing);
            }


            EntityMirror mirror = root.AddComponent<EntityMirror>();
            _mirrors.Add(mirror);

            return mirror;
        }

        public void UpdateVariables(bool isOwner) {
            _ = isOwner; // while being worked on, silence isOwner unused param

            //foreach (var fsm in fsms) {
            //    var vars = fsm.FsmVariables;
            //    //vars.GetGam
            //}
        }
        public void ToggleFSMs(bool enabled) {
            //fsms ??= Root.GetComponents<PlayMakerFSM>();
            Logger.Msg("toggling fsms:", enabled);
            fsmsEnabled = enabled;
            foreach (var fsm in Fsms) {
                fsm.enabled = enabled;
            }

        }

        public void EnableFSMs() {
            if (fsmsEnabled)
                return;

            ToggleFSMs(true);
        }
        public void DisableFSMs() {
            if (!fsmsEnabled)
                return;

            ToggleFSMs(false);
        }
        protected override void PreTick(float dt) {
            if (fsmsEnabled && Fsms.Any(fsm => fsm.enabled == false))
                EnableFSMs();
            else if (!fsmsEnabled && Fsms.Any(fsm => fsm.enabled == true))
                DisableFSMs();
        }

        public void PlayClip(PlayClipPacket packet) {
            using (new StackFlag<None>()) {
                Animator.Play(Animator.Library.GetClipByName(packet.clipName), packet.clipStartTime, packet.overrideFps);
            }
        }

        protected override void OnStart() {
            Logger.Msg("Event:", MethodInfo.GetCurrentMethod().Name, "NetOwner:", Owner?.ClientName, $"({Owner?.ClientID})");
        }
        protected override void OwnershipInitialized(Weaver owner) {
            Logger.Msg("Event:", MethodInfo.GetCurrentMethod().Name, "NetOwner:", Owner?.ClientName, $"({Owner?.ClientID})");
        }
        protected override void OnOwnershipChanged(Weaver prev, Weaver owner) {
            Logger.Msg("Event:", MethodInfo.GetCurrentMethod().Name, "NetOwner:", Owner?.ClientName, $"({Owner?.ClientID})");
            ToggleFSMs(IsLocalOwned);
            //UpdateVariables(IsLocalOwned);
        }
    }
}
