using System;
using Microsoft.Xna.Framework;

namespace FinalProject.Battle
{
    // the crouch-and-launch a teacher does when he leaves the screen. shared so
    // David's haymaker and the run up to his ultimate are literally the same
    // motion — it's the same character doing the same thing, and two copies of
    // these numbers would drift apart the moment either one got retuned.
    //
    // only the pose lives here. how far "off screen" actually is depends on who
    // is asking: PunchPattern measures it off the rig it assembled, BattleState
    // off the sprite standing above the menu box — so each passes the rise
    // through its own travel distance rather than sharing one in pixels.
    public static class TeacherLeap
    {
        public const float ChargeSeconds = 0.16f; // the crouch
        public const float LaunchSeconds = 0.30f; // the rise
        public const float TotalSeconds  = ChargeSeconds + LaunchSeconds;

        // a deep crouch, deeper than a punch wind up — this is loading a jump,
        // not coiling a hit. applied around a pivot at his feet, so squashing Y
        // sinks him into the floor without needing a separate dip
        public const float ChargeScaleX = 1.35f;
        public const float ChargeScaleY = 0.55f;

        // and the stretch it releases into, along the direction of travel
        public const float StretchX = 0.80f;
        public const float StretchY = 1.35f;

        // rise is 0 planted and 1 fully departed — squared rather than linear so
        // it reads as a launch instead of a lift. the stretch resolves over the
        // first half of the rise, so he's at full extension well before he
        // actually leaves frame
        public static void Pose(float t, out float rise, out float scaleX, out float scaleY)
        {
            if (t < ChargeSeconds)
            {
                float c = MathHelper.Clamp(t / ChargeSeconds, 0f, 1f);
                rise   = 0f;
                scaleX = MathHelper.Lerp(1f, ChargeScaleX, c);
                scaleY = MathHelper.Lerp(1f, ChargeScaleY, c);
                return;
            }

            float lt = MathHelper.Clamp((t - ChargeSeconds) / LaunchSeconds, 0f, 1f);
            rise = lt * lt;

            float st = MathF.Min(lt * 2f, 1f);
            scaleX = MathHelper.Lerp(ChargeScaleX, StretchX, st);
            scaleY = MathHelper.Lerp(ChargeScaleY, StretchY, st);
        }
    }
}
