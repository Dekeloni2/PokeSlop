using System;
using Microsoft.Xna.Framework;
using FinalProject.Core;

namespace FinalProject.Battle.Patterns;

// The boat attack. The box shrinks and the boat settles into place, then it
// repeats a "rev" cycle: squanch down + flush red + shake, spring back up, and
// belch one burst of smoke on the release. One windup per puff.
public class BoatPattern : IBulletPattern
{
    public float Duration => 10f;

    // ── timing ────────────────────────────────────────────────────────────────
    private const float LeadInSeconds  = 0.8f;  // wait for the box resize below to finish
    private const float WindupSeconds  = 0.6f;  // squashed + red + shaking
    private const float SpringSeconds  = 0.2f;  // pops back up, smoke fires at the end
    private const float RestSeconds    = 0.15f; // brief beat before the next windup

    // ── look tuning ───────────────────────────────────────────────────────────
    private const float SquashScaleY    = 0.6f;  // how far down it squanches
    private const float SquashInFraction = 0.45f; // portion of the windup spent easing down
    private const float SpringOvershoot  = 1.15f; // stretches past normal on release
    private const float ShakeMagnitude  = 3f;    // px of jitter at the peak of the windup

    // LeadIn waits for the boat to settle; then Windup→Spring→Rest loops, one
    // smoke burst per spring.
    private enum Phase { LeadIn, Windup, Spring, Rest, Done }
    private Phase _phase = Phase.LeadIn;
    private float _phaseTimer;

    private int _lastThird = -1;

    // ── visual state the draw code reads (see DodgePhase.Draw's boat block) ────
    public float   SquashY     { get; private set; } = 1f;
    public Color   BoatTint    { get; private set; } = Color.White;
    public Vector2 ShakeOffset { get; private set; } = Vector2.Zero;

    public void Start(DodgeContext context)
    {
        // shrinking the box — LeadInSeconds must match this duration so the
        // first windup waits until the boat is settled in place
        Rectangle box = context.CurrentBox;
        Rectangle tinyBox = new Rectangle(box.X + 30, box.Y, box.Width - 60, box.Height - 60);
        context.ResizeBoxTo(tinyBox, 0.8f);
    }

    public void Update(GameTime gameTime, DodgeContext context)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // once the turn's time is up, stop revving and let leftover smoke clear
        if (context.Elapsed >= Duration)
        {
            SetIdle();
            return;
        }

        _phaseTimer += dt;

        switch (_phase)
        {
            case Phase.LeadIn:
                // boat sits in place while the box finishes resizing
                SetIdle();
                if (_phaseTimer >= LeadInSeconds)
                    Advance(HasTimeForAnotherRev(context.Elapsed) ? Phase.Windup : Phase.Done);
                break;

            case Phase.Windup:
                // squash down, flush red, shake harder as it builds
                float wp = MathHelper.Clamp(_phaseTimer / WindupSeconds, 0f, 1f);
                // ease down into the squash over the first part of the windup so
                // there's a visible squanch, then hold it squashed while it revs
                float squashIn = MathHelper.Clamp(wp / SquashInFraction, 0f, 1f);
                SquashY  = MathHelper.SmoothStep(1f, SquashScaleY, squashIn);
                BoatTint = Color.Lerp(Color.White, Color.Red, MathHelper.Clamp(wp * 2f, 0f, 1f));
                float mag = ShakeMagnitude * wp;
                ShakeOffset = new Vector2(
                    ((float)Random.Shared.NextDouble() * 2f - 1f) * mag,
                    ((float)Random.Shared.NextDouble() * 2f - 1f) * mag);
                if (_phaseTimer >= WindupSeconds) Advance(Phase.Spring);
                break;

            case Phase.Spring:
                // overshoot up past normal, then settle; fade red back out
                float sp = MathHelper.Clamp(_phaseTimer / SpringSeconds, 0f, 1f);
                SquashY = sp < 0.5f
                    ? MathHelper.Lerp(SquashScaleY, SpringOvershoot, sp / 0.5f)
                    : MathHelper.Lerp(SpringOvershoot, 1f, (sp - 0.5f) / 0.5f);
                BoatTint    = Color.Lerp(Color.Red, Color.White, sp);
                ShakeOffset = Vector2.Zero;
                if (_phaseTimer >= SpringSeconds)
                {
                    SpawnSmoke(context, context.CurrentBox); // belch on the release
                    Advance(Phase.Rest);
                }
                break;

            case Phase.Rest:
                // don't start another rev if it can't finish and fire in time —
                // no point squashing for a puff that never comes
                SetIdle();
                if (_phaseTimer >= RestSeconds)
                    Advance(HasTimeForAnotherRev(context.Elapsed) ? Phase.Windup : Phase.Done);
                break;

            case Phase.Done:
                // out of time for more revs; sit idle while leftover smoke clears
                SetIdle();
                break;
        }
    }

    // A rev is only worth starting if the windup and spring can both finish
    // before the turn's time runs out; otherwise the boat would squash for a
    // burst that Update would cut off before it ever spawns.
    private bool HasTimeForAnotherRev(float elapsed)
        => elapsed + WindupSeconds + SpringSeconds <= Duration;

    private void Advance(Phase next)
    {
        _phase      = next;
        _phaseTimer = 0f;
    }

    private void SetIdle()
    {
        SquashY     = 1f;
        BoatTint    = Color.White;
        ShakeOffset = Vector2.Zero;
    }

    private void SpawnSmoke(DodgeContext context, Rectangle box)
    {
        // Calculate 1/3 of the box width
        float smokeWidth = box.Width / 3f;

        // which location the smoke will spawn, prevents a repeat of same one in a row
        int currentThird;
        do
        {
            currentThird = Random.Shared.Next(3);
        }
        while (currentThird == _lastThird);

        _lastThird = currentThird;

        // Find the center X of that specific third
        float targetX = box.Left + (currentThird * smokeWidth) + (smokeWidth / 2f);

        int smokeDensity = 4;
        float startX = targetX - (smokeWidth * 0.5f);
        float spacing = smokeWidth / (smokeDensity - 1);

        for (int i = 0; i < smokeDensity; i++)
        {
            Vector2 spawnPos = new Vector2(startX + (i * spacing), box.Bottom);
            Vector2 velocity = new Vector2(0f, -GameSettings.DodgeProjectileSpeed * 0.7f);

            context.SpawnProjectile(spawnPos, velocity, ProjectileType.Smoke);
        }
    }
}
