using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FinalProject.Core;
using FinalProject.Core.Audio;
using FinalProject.Core.Graphics;

namespace FinalProject.Battle.Patterns;

// the napoleon attack. box grows over the whole screen (HP bar gets hidden),
// camera pans, then napoleon rises up from the bottom with smoke, screen shakes
// when he lands, then he sweeps left.
// only the white pixels of the sprite hurt, see DodgePhase.CheckBeamDamage
public class NapoleonPattern : IBulletPattern
{
    public float Duration => 18f;

    // ── timing ───────────────────────────────────────────────────────────────
    private const float ExpandSeconds = 1.0f; // box grows out from the player
    private const float HoldSeconds   = 0.4f; // small pause before the camera moves
    private const float PanSeconds    = 1.2f; // camera slides over
    private const float RiseSeconds   = 2.0f; // napoleon road comes up into place
    private const float QuakeSeconds  = 0.6f; // screen shake when he lands

    // box is bigger than the screen on purpose, that way its edges (and the
    // walls that stop the soul) are never on screen no matter where the camera
    // is. otherwise you bump into an invisible wall in the middle of nowhere
    private const float ArenaWidthScale  = 1.7f; // x screen width
    private const float ArenaHeightScale = 1.5f; // x screen height

    // ── look ─────────────────────────────────────────────────────────────────
    private const float CameraPanX     = 160f; // + moves the view right
    private const float QuakeMagnitude = 14f;  // px of screen shake on landing
    private const float RiseShake      = 4f;   // px of sprite jitter while rising
    private const float SweepSpeed     = 120f; // px/sec of the final pass
    private const float SmokeInterval  = 0.04f;
    private const int   SmokePerBurst  = 3;    // puffs spawned each interval
    private const float SmokeDrift     = 20f;  // px/sec sideways wander
    private const float SmokeRiseMin   = 20f;  // px/sec upward
    private const float SmokeRiseRange = 30f;
    private const float SmokeLife      = 1.5f;
    private const float SmokeFadeIn    = 0.4f;
    private const int   SmokeMinSize   = 12;   // the art is 16x16, varied a bit
    private const int   SmokeSizeRange = 14;
    private const int   SmokeBandHeight = 60;  // vertical scatter at the spawn line
    private const int   SmokeBandOffset = 50;  // px up from the bottom of the screen

    // how far across he parks. higher = more room on the left to dodge
    private const float RestXFraction  = 0.55f;

    // his size as multiples of the screen. wider also means a longer sweep
    private const float SpriteWidthScale  = 2.2f;
    private const float SpriteHeightScale = 1.1f;

    private const string RumbleSound = "rumble"; // loops while rising
    private const string ThudSound   = "thud";   // one shot when he lands

    private enum Phase { Expand, Hold, Pan, Rise, Quake, Sweep, Done }
    private Phase _phase = Phase.Expand;
    private float _phaseTimer;
    private float _smokeTimer;

    private Beam _beam;
    private int  _restX, _restY, _startY;

    public void Start(DodgeContext context)
    {
        // grow out from wherever the soul is so the box opens around the player.
        // ends up bigger than the screen on purpose, see ArenaWidthScale
        Vector2 soul = context.HitboxPosition;
        int w = (int)(GameSettings.WindowWidth  * ArenaWidthScale);
        int h = (int)(GameSettings.WindowHeight * ArenaHeightScale);

        var arena = new Rectangle((int)soul.X - w / 2, (int)soul.Y - h / 2, w, h);

        context.ResizeBoxTo(arena, ExpandSeconds);

        // box covers the HP bar on purpose, so just hide it. the outline stays
        // on while it grows and gets hidden after (see the Expand case below)
        context.SetHudHidden(true);
    }

    public void Update(GameTime gameTime, DodgeContext context)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _phaseTimer += dt;

        switch (_phase)
        {
            case Phase.Expand:
                if (_phaseTimer >= ExpandSeconds)
                {
                    // done growing, hide the outline (its edges are off screen
                    // by now anyway)
                    context.SetBoxBorderHidden(true);
                    Advance(Phase.Hold);
                }
                break;

            case Phase.Hold:
                // small pause before the camera moves
                if (_phaseTimer >= HoldSeconds) Advance(Phase.Pan);
                break;

            case Phase.Pan:
                float p = MathHelper.Clamp(_phaseTimer / PanSeconds, 0f, 1f);
                context.SetCameraPan(new Vector2(MathHelper.SmoothStep(0f, CameraPanX, p), 0f));
                if (p >= 1f)
                {
                    SpawnNapoleon(context);
                    SoundManager.StartLoop(RumbleSound);
                    Advance(Phase.Rise);
                }
                break;

            case Phase.Rise:
                UpdateRise(context, dt);
                if (_phaseTimer >= RiseSeconds)
                {
                    SettleAtRest();
                    // rumble stops the moment he lands, thud + shake same frame
                    SoundManager.StopLoop(RumbleSound);
                    SoundManager.Play(ThudSound);
                    context.ShakeScreen(QuakeMagnitude, QuakeSeconds);
                    Advance(Phase.Quake);
                }
                break;

            case Phase.Quake:
                // DodgePhase fades the shake out by itself, just wait here
                if (_phaseTimer >= QuakeSeconds) Advance(Phase.Sweep);
                break;

            case Phase.Sweep:
                UpdateSweep(context, dt);
                break;
        }
    }

    private void Advance(Phase next)
    {
        _phase      = next;
        _phaseTimer = 0f;
    }

    private void SpawnNapoleon(DodgeContext context)
    {
        Spritesheet sheet = SpriteManager.GetSprite("napoleon");
        if (sheet == null) return; // no sprite, attack just plays empty

        int width  = (int)(GameSettings.WindowWidth  * SpriteWidthScale);
        int height = (int)(GameSettings.WindowHeight * SpriteHeightScale);

        // park him on the right side of the visible screen so the left is still
        // dodgeable. based off the camera pan and not the box, the box goes way
        // off screen
        _restX  = (int)CameraPanX + (int)(GameSettings.WindowWidth * RestXFraction);
        _restY  = 0;
        _startY = GameSettings.WindowHeight + 40; // just under the screen

        _beam = context.AddBeam(new Rectangle(_restX, _startY, width, height), sheet.Texture);
    }

    // comes up into place, jittering, with smoke
    private void UpdateRise(DodgeContext context, float dt)
    {
        if (_beam == null) return;

        float t = MathHelper.Clamp(_phaseTimer / RiseSeconds, 0f, 1f);
        int   y = (int)MathHelper.SmoothStep(_startY, _restY, t);

        int jx = (int)(((float)Random.Shared.NextDouble() * 2f - 1f) * RiseShake);
        int jy = (int)(((float)Random.Shared.NextDouble() * 2f - 1f) * RiseShake);

        Rectangle b = _beam.Bounds;
        _beam.Bounds = new Rectangle(_restX + jx, y + jy, b.Width, b.Height);

        EmitSmoke(context, dt);
    }

    private void SettleAtRest()
    {
        if (_beam == null) return;

        Rectangle b = _beam.Bounds;
        _beam.Bounds = new Rectangle(_restX, _restY, b.Width, b.Height);
    }

    private void UpdateSweep(DodgeContext context, float dt)
    {
        if (_beam == null) { Advance(Phase.Done); return; }

        Rectangle b = _beam.Bounds;
        _beam.Bounds = new Rectangle(b.X - (int)(SweepSpeed * dt), b.Y, b.Width, b.Height);

        // done once he's past the left edge of the screen. the box goes further
        // left than that so don't wait for it
        if (_beam.Bounds.Right < CameraPanX)
        {
            context.RemoveBeam(_beam);
            _beam = null;
            Advance(Phase.Done);
        }
    }

    // smoke while he rises. particles don't do damage, they're just visual
    private void EmitSmoke(DodgeContext context, float dt)
    {
        _smokeTimer += dt;
        if (_smokeTimer < SmokeInterval) return;
        _smokeTimer = 0f;

        Rectangle b = _beam.Bounds;

        // spawn along the bottom of the screen, across whatever part of him is
        // visible. his own bottom edge is ~50px under the screen since he's
        // taller than it, so spawning there would put every puff off screen
        float left  = Math.Max(b.Left,  CameraPanX);
        float right = Math.Min(b.Right, CameraPanX + GameSettings.WindowWidth);
        if (right <= left) return;

        Spritesheet smoke = SpriteManager.GetSprite("smoke");
        Texture2D smokeTex = smoke?.Texture; // null just draws a plain square

        for (int i = 0; i < SmokePerBurst; i++)
        {
            var pos = new Vector2(
                left + (float)Random.Shared.NextDouble() * (right - left),
                GameSettings.WindowHeight - SmokeBandOffset
                    + (float)Random.Shared.NextDouble() * SmokeBandHeight);

            var vel = new Vector2(
                ((float)Random.Shared.NextDouble() * 2f - 1f) * SmokeDrift,
                -SmokeRiseMin - (float)Random.Shared.NextDouble() * SmokeRiseRange);

            int size = SmokeMinSize + Random.Shared.Next(SmokeSizeRange);

            context.SpawnParticle(pos, vel, SmokeLife, size, Color.White, SmokeFadeIn, smokeTex);
        }
    }
}
