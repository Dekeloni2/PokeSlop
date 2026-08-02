using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FinalProject.Core;
using FinalProject.Core.Audio;
using FinalProject.Core.Graphics;

namespace FinalProject.Battle.Patterns;

// the boat attack. box shrinks, boat settles in place, then it repeats a cycle
// of squashing down + going red + shaking, springing back up, and letting out
// one burst of smoke when it springs. one windup per puff
//
// on top of that loop it also lobs bombs and shoots them down:
//
//   Shell  — one beat, not two. a meeting point is picked before the bomb
//            leaves the deck, and the arc is solved to put it exactly there at
//            exactly InterceptSeconds. partway through that flight the boat
//            paints a line on the same point, and the line fires the instant
//            the bomb arrives. the bomb is harmless the whole way; the laser
//            never damages either. the whole beat is about half a second
//   (blast) — a full width horizontal beam at the meeting point's height,
//            thick at first and collapsing fast. the only part that damages
//
// solving the arc against a point the laser already knows is what sells it: the
// two were authored against the same coordinates, so the hit can't drift. a
// bomb he decides not to shoot (see HoldFireChance) simply carries on through
// the point and falls out of the arena, which is where "miss" comes from
//
// the two systems share the boat's body: a Lob or an Aim uses the same squash
// and spring the smoke does, so the wind up never tells you which one is
// coming. what it costs is that the blast and the smoke can be live together,
// with the smoke pushing you up the arena and the blast wanting you out of a
// horizontal band.
public class BoatPattern : IBulletPattern
{
    public float Duration => 16f;

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

    // ── escalation ────────────────────────────────────────────────────────────
    // the first time he sets the boat up it's smoke and nothing else, so the
    // rev/belch rhythm gets to land on its own. bombs and the laser only start
    // once that's been seen — coming out swinging would bury the tell the whole
    // rest of the attack is built on.
    //
    // counted in a static because a fresh pattern is built every turn. same
    // shape ChessPattern and CloverbytePattern use, and like theirs it belongs
    // to the fight rather than the run, so BattleState clears it on OnEnter
    private const int ShellsFromUse = 2;
    private static int _timesUsed;

    public static void ResetUseCount() => _timesUsed = 0;

    private static bool ShellsUnlocked => _timesUsed >= ShellsFromUse;

    // ── bombs ─────────────────────────────────────────────────────────────────
    private const float Gravity   = 900f; // px/s², tuned with InterceptSeconds below
    private const int   MaxBombs  = 3;    // cap on how many can be in the air at once
    private const int   BombSize  = 28;   // on screen size, and the fallback square

    // every bomb is launched at a point picked before it leaves the deck, and
    // the arc is solved so it is exactly there at exactly this time. that's what
    // lets the laser be waiting at the meeting point instead of chasing the
    // bomb around — the two were authored against the same coordinates
    private const float InterceptSeconds = 0.55f;

    // ── laser ─────────────────────────────────────────────────────────────────
    // the aim window is deliberately short and sits INSIDE the flight, ending
    // exactly at the intercept. so the whole beat is lob → line → hit in about
    // half a second, and it reads as one shot rather than two separate events
    private const float AimSeconds   = 0.22f;
    private const float FlashSeconds = 0.1f;  // it brightens, then the bomb goes
    private const float HoldFireChance = 0.25f; // he just lets this one sail past

    // ── the blast ─────────────────────────────────────────────────────────────
    private const int   BlastStartHeight = 54;   // tall enough that you have to be clear of the band
    private const float BlastSeconds     = 0.3f; // collapses to nothing this fast
    private const int   BlastDamage      = 6;

    // ── sound ─────────────────────────────────────────────────────────────────
    // all borrowed from the vocabulary the other attacks already use, so none of
    // this teaches the player a new noise:
    //
    // the rev is the one that isn't a one shot. it has to last exactly as long
    // as the squash does, however that gets retuned, so it's a loop the way
    // Napoleon's rise is rather than a clip fired and hoped for
    private const string RevSound      = "rumble";          // engine winding up, looped
    private const string BelchSound    = "thud";            // the body slamming back down
    private const string LaunchSound   = "slash";           // same cue the punch's leap uses
    private const string AimSound      = "snd_spearrise";   // the telegraph, as everywhere else
    private const string DetonateSound = "snd_heavydamage"; // the payoff, as the hexes use
    private const string ShakeSound    = "snd_screenshake"; // rides with the detonation's shake

    // LeadIn waits for the boat to settle; then Windup→Spring→Rest loops. what
    // the spring actually does is decided per rev, see PickAction
    private enum Phase { LeadIn, Windup, Spring, Rest, Done }
    private Phase _phase = Phase.LeadIn;
    private float _phaseTimer;

    // what this rev's spring will do. rolled at the start of each windup so the
    // squash itself gives nothing away
    private enum Action { Smoke, Shell }
    private Action _action = Action.Smoke;

    private int _lastThird = -1;

    // ── visual state the draw code reads (see DodgePhase.Draw's boat block) ────
    public float   SquashY     { get; private set; } = 1f;
    public Color   BoatTint    { get; private set; } = Color.White;
    public Vector2 ShakeOffset { get; private set; } = Vector2.Zero;

    private sealed class Bomb
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public float   Age;       // time since launch, and the pulse's clock

        // the meeting point, decided at launch. the arc is solved to put the
        // bomb here at Age == InterceptSeconds, and the laser is aimed here
        public Vector2 Intercept;

        public bool Doomed;       // the boat means to shoot this one
        public bool ShotStarted;  // its aim window has already opened
    }

    // a shot in progress. Target is a fixed point in the world, worked out when
    // the shot was lined up — it deliberately does NOT follow the bomb, because
    // the bomb arriving there is the point. null when the boat isn't shooting
    private sealed class Shot
    {
        public Vector2 Target;
        public Bomb    Bomb;    // null when this one is a deliberate miss
        public float   Timer;
        public bool    Fired;
    }

    // a live blast band. several can overlap if bombs go off close together
    private sealed class Blast
    {
        public float Y;
        public float Timer;
        public bool  HasHit;   // one tick per blast, it isn't a lingering beam
    }

    private readonly List<Bomb>  _bombs  = new();
    private readonly List<Blast> _blasts = new();
    private Shot _shot;

    public void Start(DodgeContext context)
    {
        context.SetTeacherVisible(true);

        // counted before anything reads it, so the first setup is use 1
        _timesUsed++;

        // shrinking the box. LeadInSeconds has to match this number so the
        // first windup waits until the boat is actually in place
        Rectangle box = context.CurrentBox;
        Rectangle tinyBox = new Rectangle(box.X + 30, box.Y, box.Width - 60, box.Height - 60);
        context.ResizeBoxTo(tinyBox, 0.8f);
    }

    public void Update(GameTime gameTime, DodgeContext context)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // the bombs already in the air, the shot already lined up and the bands
        // already going off all keep running after the boat stops revving —
        // cutting them mid flight would strand a telegraph with no payoff
        UpdateBombs(dt, context);
        UpdateShot(dt, context);
        UpdateBlasts(dt, context);

        // once the turn's time is up, stop revving and let leftover smoke clear
        if (context.Elapsed >= Duration)
        {
            SetIdle();

            // this path skips Advance, so the rev loop has to be cut by hand or
            // the engine keeps running after the turn is over
            SoundManager.StopLoop(RevSound);
            _phase = Phase.Done;
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
                    Release(context);
                    Advance(Phase.Rest);
                }
                break;

            case Phase.Rest:
                // don't start another rev if there isn't time to finish it, no
                // point squashing for a puff that never happens
                SetIdle();
                if (_phaseTimer >= RestSeconds)
                {
                    if (HasTimeForAnotherRev(context.Elapsed))
                    {
                        _action = PickAction();
                        Advance(Phase.Windup);
                    }
                    else Advance(Phase.Done);
                }
                break;

            case Phase.Done:
                // out of time for more revs; sit idle while leftover smoke clears
                SetIdle();
                break;
        }
    }

    // the turn is over once the boat has stopped and nothing it threw is still
    // in play. without the bomb/shot/blast half of this the turn could end with
    // a bomb mid arc or a line painted at something that never went off
    public bool IsComplete
        => _phase == Phase.Done && _shot == null && _blasts.Count == 0 && _bombs.Count == 0;

    // what the next rev does. a shot needs something to shoot at, and there's no
    // point lobbing past the cap — anything else falls back to smoke, which is
    // the attack's baseline
    private Action PickAction()
    {
        // first time he sets this up, it's a smoke attack and nothing else
        if (!ShellsUnlocked) return Action.Smoke;

        bool canShell = _bombs.Count < MaxBombs;
        return canShell && Random.Shared.NextDouble() < 0.5 ? Action.Shell : Action.Smoke;
    }

    // the spring is the moment of release, whatever this rev was for
    private void Release(DodgeContext context)
    {
        switch (_action)
        {
            // the lob and the shot are one beat now, not two revs — LobBomb
            // schedules both, see there
            case Action.Shell:
                SoundManager.Play(LaunchSound);
                LobBomb(context);
                break;

            default:
                SoundManager.Play(BelchSound);
                SpawnSmoke(context, context.CurrentBox);
                break;
        }
    }

    // only worth starting a rev if the windup and spring both fit before the
    // turn ends, otherwise Update cuts it off before the smoke ever spawns
    private bool HasTimeForAnotherRev(float elapsed)
        => elapsed + WindupSeconds + SpringSeconds <= Duration;

    // every phase change goes through here, which makes it the one place the
    // rev loop can be kept honest — it runs for exactly the windup and stops
    // on the way into anything else, including Done
    private void Advance(Phase next)
    {
        if (next == Phase.Windup) SoundManager.StartLoop(RevSound);
        else                      SoundManager.StopLoop(RevSound);

        _phase      = next;
        _phaseTimer = 0f;
    }

    private void SetIdle()
    {
        SquashY     = 1f;
        BoatTint    = Color.White;
        ShakeOffset = Vector2.Zero;
    }

    // ── bombs ─────────────────────────────────────────────────────────────────

    // where the boat's deck sits, just under the arena — bombs come up out of
    // here and the aim line starts here, so both read as coming from the boat
    private static Vector2 Muzzle(Rectangle box) => new(box.Center.X, box.Bottom + 8f);

    private void LobBomb(DodgeContext context)
    {
        Rectangle box = context.CurrentBox;
        Vector2 from = Muzzle(box);

        // the meeting point, picked before the shell leaves the deck. kept off
        // the edges so the bomb sprite can't hang half outside the arena, and
        // out of the bottom third so the blast band isn't always underfoot
        int half = BombSize / 2;
        float toX = Random.Shared.Next(box.Left + half, box.Right - half);
        float toY = Random.Shared.Next(box.Top + half, box.Center.Y);
        var intercept = new Vector2(toX, toY);

        // solve the arc to put it exactly there at exactly InterceptSeconds:
        // horizontal is a straight divide, vertical is whatever launch speed
        // gets it to toY in that time under Gravity. if nobody shoots it, it
        // simply carries on through and falls out of the arena
        const float t = InterceptSeconds;
        float vx = (toX - from.X) / t;
        float vy = (toY - from.Y - 0.5f * Gravity * t * t) / t;

        _bombs.Add(new Bomb
        {
            Position  = from,
            Velocity  = new Vector2(vx, vy),
            Intercept = intercept,
            // decided now rather than at the intercept, so a bomb he never
            // meant to shoot is a bomb that simply sails past
            Doomed    = Random.Shared.NextDouble() >= HoldFireChance,
        });

        context.ShakeScreen(2f, 0.12f);
    }

    private void UpdateBombs(float dt, DodgeContext context)
    {
        float floorY = context.CurrentBox.Bottom - BombSize / 2f;

        for (int i = _bombs.Count - 1; i >= 0; i--)
        {
            Bomb bomb = _bombs[i];
            bomb.Age += dt;

            bomb.Velocity.Y += Gravity * dt;
            bomb.Position   += bomb.Velocity * dt;

            // open the aim window so it CLOSES on the intercept — the line goes
            // up while the bomb is still climbing toward the point, and fires
            // the moment it arrives. one shot at a time; a bomb whose window
            // comes up while the laser is busy just doesn't get shot
            if (bomb.Doomed && !bomb.ShotStarted && _shot == null
                && bomb.Age >= InterceptSeconds - AimSeconds)
            {
                bomb.ShotStarted = true;
                _shot = new Shot { Bomb = bomb, Target = bomb.Intercept };
                SoundManager.Play(AimSound);
            }

            // it came down without being shot. gone — a bomb that survived its
            // arc doesn't get to sit on the floor waiting to be detonated later.
            //
            // the Velocity check is load bearing: a bomb is launched from the
            // deck, which is BELOW the arena floor, so it starts out already
            // past this line on the way up. without "only while falling" every
            // bomb was deleted on its first frame — which also starved the
            // laser, since it has nothing to aim at unless a bomb is airborne
            if (bomb.Velocity.Y > 0f && bomb.Position.Y >= floorY) _bombs.RemoveAt(i);
        }
    }


    // ── the shot ──────────────────────────────────────────────────────────────

    private void UpdateShot(float dt, DodgeContext context)
    {
        if (_shot == null) return;

        _shot.Timer += dt;

        if (!_shot.Fired && _shot.Timer >= AimSeconds)
        {
            _shot.Fired = true;

            // the bomb should be sitting right on the crosshair by now, since
            // the arc was solved to put it there. Remove doubles as the check:
            // if it somehow left first, the shot resolves into nothing
            if (_shot.Bomb != null && _bombs.Remove(_shot.Bomb))
                Detonate(_shot.Target.Y, context);
        }

        if (_shot.Timer >= AimSeconds + FlashSeconds) _shot = null;
    }

    // the blast sits at the point that was painted, not at wherever the bomb
    // drifted to — the telegraph is the promise, so it has to be what pays out
    private void Detonate(float y, DodgeContext context)
    {
        _blasts.Add(new Blast { Y = y });

        // the two cues land on the same frame on purpose — the same pairing the
        // punch's strike and the hexagons' explosion use
        SoundManager.Play(DetonateSound);
        SoundManager.Play(ShakeSound);
        context.ShakeScreen(8f, 0.25f);
    }

    // ── the blast ─────────────────────────────────────────────────────────────

    private void UpdateBlasts(float dt, DodgeContext context)
    {
        for (int i = _blasts.Count - 1; i >= 0; i--)
        {
            Blast blast = _blasts[i];
            blast.Timer += dt;

            // one hit per blast rather than a damage cadence — it's a single
            // sweep, not a beam you can be held inside
            if (!blast.HasHit && BlastBounds(blast).Contains(context.HitboxPosition.ToPoint()))
            {
                blast.HasHit = true;
                context.DamagePlayer(BlastDamage);
            }

            if (blast.Timer >= BlastSeconds) _blasts.RemoveAt(i);
        }
    }

    // full window width — the only way past one of these is to not be at its
    // height when it goes off, so clipping it to the arena would be a lie about
    // where it reaches
    private static Rectangle BlastBounds(Blast blast)
    {
        float t = MathHelper.Clamp(blast.Timer / BlastSeconds, 0f, 1f);

        // collapses toward its own centre line, fast at first
        int height = (int)(BlastStartHeight * (1f - t) * (1f - t));

        return new Rectangle(0, (int)(blast.Y - height / 2f), GameSettings.WindowWidth, Math.Max(height, 1));
    }

    // ── drawing ───────────────────────────────────────────────────────────────

    // the boat's own body is drawn behind the arena by DodgePhase, off the three
    // properties above. everything this pattern owns goes in front of the
    // bullets instead, so a bomb can't be lost behind a cloud of smoke
    public void Draw(SpriteBatch spriteBatch, Texture2D pixel, DodgeContext context)
    {
        DrawBombs(spriteBatch, pixel);
        DrawShot(spriteBatch, pixel, context);
        DrawBlasts(spriteBatch, pixel);
    }

    private void DrawBombs(SpriteBatch spriteBatch, Texture2D pixel)
    {
        Texture2D tex = SpriteManager.GetSprite("bomb")?.Texture;

        foreach (Bomb bomb in _bombs)
        {
            // a slow flicker so a bomb crossing a cloud of smoke stays findable
            float pulse = 0.8f + 0.2f * MathF.Sin(bomb.Age * MathHelper.TwoPi * 2.2f);

            var dst = new Rectangle(
                (int)(bomb.Position.X - BombSize / 2f),
                (int)(bomb.Position.Y - BombSize / 2f),
                BombSize, BombSize);

            if (tex != null) spriteBatch.Draw(tex, dst, Color.White * pulse);
            else             spriteBatch.Draw(pixel, dst, Color.OrangeRed * pulse);
        }
    }

    private void DrawShot(SpriteBatch spriteBatch, Texture2D pixel, DodgeContext context)
    {
        if (_shot == null) return;

        Vector2 from = Muzzle(context.CurrentBox);

        // thin and dim while it's only a warning, then a bright thick flash on
        // the frame it actually fires. the line never damages either way
        bool flash = _shot.Fired;
        int thickness = flash ? 4 : 1;
        Color color = flash ? Color.White : new Color(255, 80, 80) * 0.75f;

        DrawLine(spriteBatch, pixel, from, _shot.Target, thickness, color);
    }

    private void DrawBlasts(SpriteBatch spriteBatch, Texture2D pixel)
    {
        foreach (Blast blast in _blasts)
        {
            Rectangle bounds = BlastBounds(blast);
            float fade = 1f - MathHelper.Clamp(blast.Timer / BlastSeconds, 0f, 1f);

            spriteBatch.Draw(pixel, bounds, Color.White * MathHelper.Clamp(fade + 0.35f, 0f, 1f));
        }
    }

    private static void DrawLine(SpriteBatch spriteBatch, Texture2D pixel,
        Vector2 a, Vector2 b, int thickness, Color color)
    {
        Vector2 delta = b - a;
        float length = delta.Length();
        if (length < 0.001f) return;

        float angle = MathF.Atan2(delta.Y, delta.X);

        spriteBatch.Draw(pixel, a, null, color, angle,
            new Vector2(0f, 0.5f), new Vector2(length, thickness), SpriteEffects.None, 0f);
    }

    // ── smoke ─────────────────────────────────────────────────────────────────

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
