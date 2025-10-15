using SilkBound.Managers;
using SilkBound.Network.Packets.Impl.Mirror;
using SilkBound.Network.Packets.Impl.Sync.World;
using SilkBound.Types;
using SilkBound.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SilkBound.Network
{
    public class LocalWeaver : Weaver
    {
        public LocalWeaver(string name, NetworkConnection? connection = null, Guid? clientID = null) : base(name, connection, clientID)
        {
            
        }

        void ListenAnimationCompleted(ref Action<tk2dSpriteAnimator, tk2dSpriteAnimationClip> action, Action<tk2dSpriteAnimator, tk2dSpriteAnimationClip> listener)
        {
            if (action == null || !action.GetInvocationList().Contains(listener))
                action += listener;
        }

        private void EmoteResetState(tk2dSpriteAnimator arg1, tk2dSpriteAnimationClip arg2)
        {
            if (!IsLocal)
                return;

            Logger.Msg("Resetting state");

            HeroController hero = HeroController.instance;
            hero.StartAnimationControl();
            hero.AcceptInput();
        }

        /// <summary>
        /// "Shaw!" emote with a little animation.
        /// </summary>
        public void Shaw()
        {
            if (!IsLocal)
                return;

            string clipName = "Taunt";
            HeroController hero = HeroController.instance;
            if (!hero.cState.onGround)
                return;

            HeroAnimationController animator = hero.animCtrl;
            if (animator.CurrentClipNameContains(clipName))
                return;

            tk2dSpriteAnimationClip clip = animator.GetClip(clipName);

            hero.IgnoreInput();
            //hero.rb2d.linearVelocity = Vector2.zero;
            hero.Move(0, true);
            hero.StopAnimationControl();
            ListenAnimationCompleted(ref animator.animator.AnimationCompleted, EmoteResetState);
            animator.animator.Play(clip);

            SoundManager.PlayNetworked(new PlaySoundPacket("shaw", HeroController.instance.transform.position, 30, 45, 15f), HeroController.instance.AudioCtrl.jump.transform.position, volume: 15f);
        }
    }
}
