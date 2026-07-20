using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FinalProject.Core;
using FinalProject.Core.Graphics;

namespace FinalProject.Battle.Patterns;

// Napoleon. The arena swells to swallow the whole screen (including the HP
// readout, which is hidden outright for the attack), the camera pans over, then
// Napoleon grinds up from below in a cloud of smoke, the screen quakes as he
// lands, and finally he sweeps left across the arena.
//
// Only his WHITE pixels hurt — DodgePhase.CheckBeamDamage runs a per-pixel test
// for this pattern specifically, so the dark areas are safe to sit in.
public class NapoleonPattern : IBulletPattern
{
    public float Duration => 18f;

    // ── timing ───────────────────────────────────────────────────────────────
    private const float ExpandSeconds = 1.0f; // arena swells out from the player
    private const float HoldSeconds   = 0.4f; // beat once it's open, before the pan
    private const float PanSeconds    = 1.2f; // camera slides over
    private const float RiseSeconds   = 2.0f; // Napoleon grinds up into place
    private const float QuakeSeconds  = 0.6f; // everything shakes once he lands

    // The arena is deliberately bigger than the screen so its edges — and the
    // walls that clamp the soul — stay off-screen for every camera position.
    // The player never meets an invisible wall inside the visible area.
    private const float ArenaWidthScale  = 1.7f; // × screen width
    private const float ArenaHeightScale = 1.5f; // × screen height

    // ── look / feel ──────────────────────────────────────────────────────────
    private const float CameraPanX     = 160f; // + shifts the view right (content left)
    private const float QuakeMagnitude = 14f;  // px of screen shake on landing
    private const float RiseShake      = 4f;   // px of sprite judder while rising
    private const float SweepSpeed     = 120f; // px/sec of the final pass
    private const float SmokeInterval  = 0.04f;

    // Where Napoleon parks, as a fraction across the arena. Higher = further
    // right = more room on the left for the player to dodge into.
    private const float RestXFraction  = 0.55f;

    // His footprint, as multiples of the screen. Wider also means a longer
    // sweep, since he has to fully clear the arena before the turn can end.
    private const float SpriteWidthScale  = 2.2f;
    private const float SpriteHeightScale = 1.1f;

    private enum Phase { Expand, Hold, Pan, Rise, Quake, Sweep, Done }
    private Phase _phase = Phase.Expand;
    private float _phaseTimer;
    private float _smokeTimer;

    private Beam _beam;
    private int  _restX, _restY, _startY;

    public void Start(DodgeContext context)
    {
        // Swell outward from wherever the soul is standing, so the arena opens
        // up around the player. It ends up larger than the screen on purpose —
        // see ArenaWidthScale — so its edges stay out of view once the camera
        // pans, and the player never runs into an unseen wall.
        Vector2 soul = context.HitboxPosition;
        int w = (int)(GameSettings.WindowWidth  * ArenaWidthScale);
        int h = (int)(GameSettings.WindowHeight * ArenaHeightScale);

        var arena = new Rectangle((int)soul.X - w / 2, (int)soul.Y - h / 2, w, h);

        context.ResizeBoxTo(arena, ExpandSeconds);

        // The arena swallows the HP bar by design. The outline stays visible
        // while the box swells — that growth is part of the show — and is
        // dropped once it has settled (see the Open→Rise transition).
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
                    // fully open (and its edges are off-screen by now anyway) —
                    // drop the outline so nothing frames the attack
                    context.SetBoxBorderHidden(true);
                    Advance(Phase.Hold);
                }
                break;

            case Phase.Hold:
                // let the wide-open arena land before the camera moves
                if (_phaseTimer >= HoldSeconds) Advance(Phase.Pan);
                break;

            case Phase.Pan:
                float p = MathHelper.Clamp(_phaseTimer / PanSeconds, 0f, 1f);
                context.SetCameraPan(new Vector2(MathHelper.SmoothStep(0f, CameraPanX, p), 0f));
                if (p >= 1f)
                {
                    SpawnNapoleon(context);
                    Advance(Phase.Rise);
                }
                break;

            case Phase.Rise:
                UpdateRise(context, dt);
                if (_phaseTimer >= RiseSeconds)
                {
                    SettleAtRest();
                    context.ShakeScreen(QuakeMagnitude, QuakeSeconds);
                    Advance(Phase.Quake);
                }
                break;

            case Phase.Quake:
                // DodgePhase decays the shake on its own; just hold here
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
        if (sheet == null) return; // asset missing — the attack just plays empty

        int width  = (int)(GameSettings.WindowWidth  * SpriteWidthScale);
        int height = (int)(GameSettings.WindowHeight * SpriteHeightScale);

        // Park him toward the right of the *visible screen* so the left stays
        // dodgeable. Measured off the camera pan rather than the arena, since
        // the arena deliberately runs well off-screen.
        _restX  = (int)CameraPanX + (int)(GameSettings.WindowWidth * RestXFraction);
        _restY  = 0;
        _startY = GameSettings.WindowHeight + 40; // just below the screen

        _beam = context.AddBeam(new Rectangle(_restX, _startY, width, height), sheet.Texture);
    }

    // Grinds upward into place, juddering, trailing smoke from its base.
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

        // done once he's fully past the left edge of the visible screen — the
        // arena extends further left than that, so don't wait for it
        if (_beam.Bounds.Right < CameraPanX)
        {
            context.RemoveBeam(_beam);
            _beam = null;
            Advance(Phase.Done);
        }
    }

    // Smoke boiling off the bottom edge of the sprite as it rises. Decorative
    // only — particles never damage the player.
    private void EmitSmoke(DodgeContext context, float dt)
    {
        _smokeTimer += dt;
        if (_smokeTimer < SmokeInterval) return;
        _smokeTimer = 0f;

        Rectangle b = _beam.Bounds;

        // Billow along the bottom of the *visible* screen, across whatever part
        // of Napoleon is on it. His own bottom edge sits ~50px below the view
        // (he's taller than the screen), so anchoring to that would spawn every
        // puff out of sight.
        float left  = Math.Max(b.Left,  CameraPanX);
        float right = Math.Min(b.Right, CameraPanX + GameSettings.WindowWidth);
        if (right <= left) return;

        Spritesheet smoke = SpriteManager.GetSprite("smoke");
        Texture2D smokeTex = smoke?.Texture; // null falls back to a plain square

        for (int i = 0; i < 3; i++)
        {
            var pos = new Vector2(
                left + (float)Random.Shared.NextDouble() * (right - left),
                GameSettings.WindowHeight - 50 + (float)Random.Shared.NextDouble() * 60);

            var vel = new Vector2(
                ((float)Random.Shared.NextDouble() * 2f - 1f) * 20f,
                -20f - (float)Random.Shared.NextDouble() * 30f);

            int size = 12 + Random.Shared.Next(14); // 16x16 art, drawn a bit varied

            context.SpawnParticle(pos, vel, 1.5f, size, Color.White, 0.4f, smokeTex);
        }
    }
}
