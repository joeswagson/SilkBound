using HarmonyLib;
using SilkBound.Behaviours;
using SilkBound.Types.Mirrors;
using UnityEngine;namespace SilkBound.Patches.Simple.Hero
{
    [HarmonyPatch(typeof(HeroAnimationController))]
    public class HeroAnimationControllerPatches
    {

        [HarmonyPrefix]
        [HarmonyPatch("Awake")]
        public static bool Spawned(HeroAnimationController __instance)
        {
            bool isMirror = HornetMirror.IsMirror(__instance.gameObject);

            __instance.animator = __instance.GetComponent<tk2dSpriteAnimator>();
            __instance.meshRenderer = __instance.GetComponent<MeshRenderer>();
            __instance.heroCtrl = isMirror ? __instance.GetComponent<HeroControllerMirror>() : __instance.GetComponent<HeroController>();
            Logger.Msg("hero:", __instance.heroCtrl, "ismirror:", isMirror);
            __instance.audioCtrl = __instance.GetComponent<HeroAudioController>();
            __instance.cState = __instance.heroCtrl.cState;
            __instance.clearBackflipSpawnedAudio = delegate ()
            {
                __instance.backflipSpawnedAudio = null;
            };

            return false;
        }

        //[HarmonyPrefix]
        //[HarmonyPatch(nameof(HeroAnimationController.Play), new Type[] { typeof(string), typeof(float) })]
        //public static bool Play_Prefix(HeroAnimationController __instance, string clipName, float speedMultiplier = 1f)
        //{
        //    //Logger.Msg("Play() called with:", clipName, "speedMultiplier:", speedMultiplier);

        //    if (__instance.animator == null)
        //    {
        //        //Logger.Msg("ERROR: animator is NULL!");
        //    }
        //    else if (__instance.animator.CurrentClip == null)
        //    {
        //        //Logger.Msg("WARNING: animator.CurrentClip is NULL!");
        //    }
        //    else
        //    {
        //        //Logger.Msg("CurrentClip name =", __instance.animator.CurrentClip.name);
        //    }

        //    if (clipName == __instance.animator?.CurrentClip?.name)
        //    {
        //        //Logger.Msg("Clip is already playing, returning early");
        //        return false;
        //    }

        //    if (NetworkUtils.LocalClient != null && __instance == HeroController.instance.GetComponent<HeroAnimationController>())
        //        NetworkUtils.SendPacket(new Network.Packets.Impl.PlayClipPacket(NetworkUtils.LocalClient.ClientID, clipName, speedMultiplier));
        //    //Logger.Msg("Sent packet.");

        //    //Logger.Msg("playingIdleRest =", __instance.playingIdleRest);
        //    if (__instance.playingIdleRest)
        //    {
        //        //Logger.Msg("ResetIdleLook() called");
        //        __instance.ResetIdleLook();
        //    }

        //    //Logger.Msg("Calling ResetPlaying()");
        //    __instance.ResetPlaying();

        //    //Logger.Msg("Fetching clip:", clipName);
        //    tk2dSpriteAnimationClip clip = __instance.GetClip(clipName);
        //    if (clip == null)
        //    {
        //        //Logger.Msg("ERROR: GetClip returned NULL for", clipName);
        //    }
        //    else
        //    {
        //        //Logger.Msg("Got clip:", clip.name, "fps:", clip.fps);
        //    }

        //    if (!Mathf.Approximately(speedMultiplier, 1f))
        //    {
        //        //Logger.Msg("Playing clip with speed multiplier:", speedMultiplier);
        //        __instance.animator?.Play(clip, 0f, clip != null ? clip.fps * speedMultiplier : 0f);
        //    }
        //    else
        //    {
        //        //Logger.Msg("Playing clip with default speed");
        //        __instance.animator?.Play(clip);
        //    }

        //    //Logger.Msg("Assigning animator delegates");
        //    if (__instance.animator != null)
        //    {
        //        __instance.animator.AnimationEventTriggered =
        //            new Action<tk2dSpriteAnimator, tk2dSpriteAnimationClip, int>(__instance.AnimationEventTriggered);
        //        __instance.animator.AnimationCompleted =
        //            new Action<tk2dSpriteAnimator, tk2dSpriteAnimationClip>(__instance.AnimationCompleteDelegate);
        //    }

        //    //Logger.Msg("isPlayingSlashLand =", __instance.isPlayingSlashLand);
        //    if (__instance.isPlayingSlashLand)
        //    {
        //        //Logger.Msg("Resetting isPlayingSlashLand + playSlashLand");
        //        __instance.isPlayingSlashLand = false;
        //        __instance.playSlashLand = false;
        //    }

        //    //Logger.Msg("checkMantleCancel =", __instance.checkMantleCancel);
        //    if (__instance.checkMantleCancel)
        //    {
        //        //Logger.Msg("Resetting playMantleCancel + playBackflip");
        //        __instance.playMantleCancel = false;
        //        __instance.playBackflip = false;
        //    }

        //    //Logger.Msg("Play() finished successfully for", clipName);

        //    return false;
        //}
    }
}
