using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FinalProject.Core;
using FinalProject.Core.Audio;
using FinalProject.Core.Graphics;
using FinalProject.Data;

namespace FinalProject.Battle.Patterns;

// David's heavy punch. He hops in from one side of the arena, coils down into
// a squash + slight rotate wind up, flashes red for a beat as a second sharper
// warning, then snaps into a big horizontal stretch that reaches over the box.
// There's no separate projectile — his own stretched body is the hazard, so
// touching any part of him while he's mid strike is what deals the damage,
// gated by the same undertale blue/orange rule Cloverbyte's lash teaches: the
// near arm (his forward, flexed one — see BuildRig, "nearArm" in the JSON is
// the limb that actually leads the reach; "farArm" is the one raised over his
// head) shows the fist's colour for the whole telegraph, so there's time to
// read it before it matters. Once he's unwound back to normal he crouches,
// rockets straight up off the top of the screen and falls back in from
// outside frame — landing on either side at random, so which side he throws
// from next isn't telegraphed by which side he used last. He repeats the
// whole hop/punch/leap cycle PunchCount times before he's actually done.
//
// Draws his own rig straight from the teacher's JSON (context.TeacherSprite)
// rather than the passive floating idle — SetTeacherVisible is never called
// here, this pattern owns his whole appearance for the length of the attack.
// The rig is drawn as a rigid group: every part keeps its offset relative to
// the others, and squash/stretch/rotate/mirror are applied to the group as a
// whole around a shared pivot planted at his feet, so a squash naturally
// keeps him grounded instead of needing separate "sink into the floor" logic.
public class PunchPattern : IBulletPattern
{
    public float Duration => _duration;

    // ── timing ───────────────────────────────────────────────────────────────
    private const float EnterSeconds    = 0.30f; // stand there a moment so his entrance actually reads
    private const float HopSeconds      = 0.22f;
    private const float WindupSeconds   = 0.40f;

    // the red flash — a second, sharper warning stacked on top of the squish,
    // and where the fist's colour actually gets shown so there's real time to
    // read it before the strike is live
    private const float TelegraphSeconds = 0.40f;
    private const float TelegraphPulseHz = 7f;

    private const float StrikeSeconds   = 0.09f; // the snap itself — fast, "heavy" comes from size not speed
    private const float HoldSeconds     = 0.16f;
    private const float RetractSeconds  = 0.28f;
    private const float UnsquishSeconds = 0.26f;
    private const float RestSeconds     = 0.30f;

    // the return trip: crouch, rocket straight up off the top of the screen,
    // then reappear above frame and fall back down — the mirror of how
    // Cloverbyte's logo arrives, run backwards for the takeoff and forwards
    // again for the landing
    // the leap itself is shared with the ultimate's intro, see TeacherLeap
    private const float LeapChargeSeconds = TeacherLeap.ChargeSeconds;
    private const float LaunchSeconds     = TeacherLeap.LaunchSeconds;
    private const float FallSeconds       = 0.50f;
    private const float LandSquashSeconds = 0.30f;

    private const float DurationSlack   = 0.3f;

    // how many times he throws the punch before he's done — each one ends
    // with the same leap, so this is also how many times he swaps sides.
    // David's ultimate borrows this pattern for a single hit, so the count is
    // a constructor argument with the standalone attack's value as the default
    private const int DefaultPunchCount = 3;

    private readonly int _punchCount;

    // standalone he walks on and stands there a beat before the first hop. as a
    // beat of the ultimate there's no room for an entrance that reads as
    // waiting, so he drops in from above frame instead — the same Fall the
    // attack already uses to swap sides, just moved to the front
    private readonly bool _entersFromSky;

    public PunchPattern(int punchCount = DefaultPunchCount, bool entersFromSky = false)
    {
        _punchCount    = punchCount;
        _entersFromSky = entersFromSky;
    }

    // for a host pattern sequencing this as one beat — Duration is its own
    // estimate of the run, this is the animation actually having finished.
    // deliberately not IBulletPattern.IsComplete: standalone, the turn should
    // still run out the clock it budgeted rather than cutting off at Done
    public bool Finished => _phase == Phase.Done;

    // ── layout ───────────────────────────────────────────────────────────────
    private const float GapIdle       = 46f; // clear of the box while he's just standing there
    private const float GapHop        = 4f;  // closed in, still not touching — the "ready" beat
    private const float LungeDistance = 54f; // extra px the pivot itself surges forward on the strike
    private const float HopArc        = 14f; // px the hop rises off the ground

    // squash & stretch — X is the punch axis (toward the box), Y is up/down.
    // StrikeScaleX is what carries him well into the box — standing still at
    // box center has to be inside his reach, otherwise the whole attack is a
    // no-op. This is the first number to retune if he's still coming up short
    // (or if he's swallowing the entire arena and leaving no safe spot at all)
    private const float WindupScaleX  = 1.22f;
    private const float WindupScaleY  = 0.74f;
    private const float WindupTiltDeg = 11f;   // coils away from the box before he throws it
    private const float StrikeScaleX  = 3.6f;
    private const float StrikeScaleY  = 1.15f;

    // the crunch on impact — Hold isn't a static freeze-frame, it's a quick
    // compress-and-release riding on top of the strike's full stretch, so the
    // hit reads as having actually landed on something solid
    private const float RecoilFraction = 0.4f;  // portion of Hold spent snapping back out
    private const float RecoilAmount   = 0.14f; // how hard the crunch bites, as a fraction of the strike scale

    // the leap — deep crouch, then a launch/fall stretch in the direction of
    // travel, then a hard landing squash that settles back to normal
    private const float LeapChargeScaleX = TeacherLeap.ChargeScaleX;
    private const float LeapChargeScaleY = TeacherLeap.ChargeScaleY;
    private const float LaunchStretchX   = TeacherLeap.StretchX;
    private const float LaunchStretchY   = TeacherLeap.StretchY;
    private const float FallStretchX     = 0.88f;
    private const float FallStretchY     = 1.20f;
    private const float LandSquashX      = 1.30f;
    private const float LandSquashY      = 0.60f;

    private const int PunchDamage = 8;

    private const string HopSound       = "thud";
    private const string TelegraphSound = "snd_spearrise";   // plays as the coil finishes, right before the snap
    private const string StrikeSound    = "snd_heavydamage";
    private const string JumpSound      = "slash";           // the crouch releasing into the launch
    private const string LandSound      = "thud";
    private const string ShakeSound     = "snd_screenshake"; // rides along with every shake, see Shake()

    // the red glow while he charges. the fist's own colour is the shared
    // blue/orange rule instead, see HazardRule
    private static readonly Color ChargeColor = new Color(255, 60, 60);

    // decided fresh each punch, so repeating the move doesn't repeat the answer.
    // never White here — the fist always plays by one of the two
    private HazardRule _fistColor;
    private Color FistBaseColor => _fistColor.Tint();

    private enum Side  { Left, Right }
    private enum Phase
    {
        Enter, Hop, Windup, Telegraph, Strike, Hold, Retract, Unsquish, Rest,
        LeapCharge, Launch, Fall, LandSquash, Done
    }

    private Phase _phase = Phase.Enter;
    private float _timer;
    private float _duration;

    private Side _side;       // which side he's currently posed/attacking from
    private Side _landSide;
    private bool _ready;      // false if the teacher has no sprite data — attack just runs out the clock
    private int  _punchesDone;

    // rig geometry, read once from the teacher's JSON at Start
    private TeacherSpriteData _rig;
    private Texture2D _texture;
    private Vector2   _rawPivot;   // feet point, in raw (unscaled) sprite space
    private Vector2[] _rawDelta;   // per-part center relative to _rawPivot, raw space
    private float     _halfWidth;  // half the rig's on screen width at scale 1
    private float     _rawHeight;  // full on screen height at scale 1 (pivot sits at the bottom of it)
    private int       _nearArmIndex = -1; // where the impact burst spawns from, see SpawnImpactBurst

    // current pose, recomputed every frame from the phase timer
    private Vector2 _bodyPos;               // screen position of the feet pivot
    private float   _scaleX = 1f, _scaleY = 1f;
    private float   _rotation;              // radians
    private bool    _visible;
    private Color   _bodyTint = Color.White; // torso/head/far arm — flashes red during Telegraph
    private Color   _fistTint = Color.White; // near arm only — shows the fist's colour rule

    // native art faces +X (right); side Right needs a mirror to face the box
    private bool  Flip     => _side == Side.Right;
    // which world direction is "toward the box" for the side he's on right now
    private float DirSign  => _side == Side.Left ? 1f : -1f;

    public void Start(DodgeContext context)
    {
        _rig = context.TeacherSprite;
        _ready = _rig is { Parts.Count: > 0 };
        if (_ready)
        {
            Spritesheet sheet = SpriteManager.GetSprite(_rig.Sheet);
            _texture = sheet?.Texture;
            _ready = _texture != null;
        }

        if (!_ready)
        {
            // no rig to animate — don't hang the turn, just let it run out
            _duration = EnterSeconds + DurationSlack;
            return;
        }

        BuildRig();

        _side = Random.Shared.Next(2) == 0 ? Side.Left : Side.Right;
        _bodyPos = new Vector2(PivotX(context, _side, GapIdle), FeetY(context));
        _scaleX = 1f; _scaleY = 1f; _rotation = 0f;
        _visible = true;

        // drop in instead of standing there. Fall already lerps from OffscreenY
        // down to his feet and hands off to LandSquash, which hands off to Hop —
        // so starting parked at the top of that is the whole entrance, no new
        // phase needed. it also means the landing shake and smoke come for free
        if (_entersFromSky)
        {
            _phase     = Phase.Fall;
            _bodyPos.Y = OffscreenY;
            _scaleX    = FallStretchX;
            _scaleY    = FallStretchY;
        }

        // every punch through the leap's charge and launch; only the ones
        // that land also pay for the fall and the squash — the last leap
        // exits mid air instead, see Phase.Launch
        float punchToLaunch = HopSeconds + WindupSeconds + TelegraphSeconds + StrikeSeconds + HoldSeconds
                             + RetractSeconds + UnsquishSeconds + RestSeconds
                             + LeapChargeSeconds + LaunchSeconds;
        float landing = FallSeconds + LandSquashSeconds;

        // arriving from the sky costs a fall and a landing instead of the
        // stand-and-wait, so the budget has to swap those too or the turn ends
        // while he's still mid animation
        float entry = _entersFromSky ? landing : EnterSeconds;
        _duration = entry + punchToLaunch * _punchCount + landing * (_punchCount - 1) + DurationSlack;
    }

    public void Update(GameTime gameTime, DodgeContext context)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _timer += dt;

        if (!_ready) return;

        switch (_phase)
        {
            case Phase.Enter:
                if (_timer >= EnterSeconds) Advance(Phase.Hop);
                break;

            case Phase.Hop:
            {
                float t = Progress(HopSeconds);
                float fromX = PivotX(context, _side, GapIdle);
                float toX   = PivotX(context, _side, GapHop);
                _bodyPos = new Vector2(
                    MathHelper.Lerp(fromX, toX, EaseOut(t)),
                    FeetY(context) - HopArc * MathF.Sin(MathF.PI * t));
                if (_timer >= HopSeconds)
                {
                    _bodyPos.Y = FeetY(context);
                    SoundManager.Play(HopSound);
                    Shake(context, 2f, 0.12f);
                    Advance(Phase.Windup);
                }
                break;
            }

            case Phase.Windup:
            {
                float t = Progress(WindupSeconds);
                _scaleX   = MathHelper.Lerp(1f, WindupScaleX, t);
                _scaleY   = MathHelper.Lerp(1f, WindupScaleY, t);
                _rotation = MathHelper.Lerp(0f, MathHelper.ToRadians(WindupTiltDeg) * -DirSign, t);
                if (_timer >= WindupSeconds)
                {
                    // decided here so the whole telegraph shows the real
                    // colour — springing a fresh one at the strike would give
                    // no time to read it
                    _fistColor = Random.Shared.Next(2) == 0 ? HazardRule.Blue : HazardRule.Orange;
                    SoundManager.Play(TelegraphSound);
                    Advance(Phase.Telegraph);
                }
                break;
            }

            case Phase.Telegraph:
            {
                // the coil holds still here — this phase is purely the flash,
                // nothing about the pose moves, so it can't be mistaken for
                // more wind up
                float t = Progress(TelegraphSeconds);
                float pulse = (MathF.Sin(t * TelegraphPulseHz * MathHelper.TwoPi) + 1f) * 0.5f;

                // builds toward a steadier colour as it goes, so the last
                // flash before the snap reads as "now" rather than blending
                // into the rest of the charge
                float amount = MathHelper.Lerp(0.4f, 1f, t) * MathHelper.Lerp(0.5f, 1f, pulse);
                _bodyTint = Color.Lerp(Color.White, ChargeColor, amount);
                _fistTint = Color.Lerp(Color.White, FistBaseColor, amount);

                if (_timer >= TelegraphSeconds)
                {
                    // the flash was the warning — the strike itself only
                    // keeps the fist's colour, the rest of him goes back to
                    // normal so the fist is the one thing left to read
                    _bodyTint = Color.White;
                    _fistTint = FistBaseColor;
                    Advance(Phase.Strike);
                }
                break;
            }

            case Phase.Strike:
            {
                // which curve drives the reach changes every punch — see
                // Ease() — so across the three hits he's literally
                // demonstrating linear, ease in and ease out back to back
                float t  = Progress(StrikeSeconds);
                float et = Ease(t);
                _scaleX   = MathHelper.Lerp(WindupScaleX, StrikeScaleX, et);
                _scaleY   = MathHelper.Lerp(WindupScaleY, StrikeScaleY, et);
                _rotation = MathHelper.Lerp(MathHelper.ToRadians(WindupTiltDeg) * -DirSign, 0f, et);
                _bodyPos.X = MathHelper.Lerp(
                    PivotX(context, _side, GapHop),
                    PivotX(context, _side, GapHop) + DirSign * LungeDistance, et);
                if (_timer >= StrikeSeconds)
                {
                    SoundManager.Play(StrikeSound);
                    Shake(context, 15f, 0.22f);
                    SpawnImpactBurst(context);
                    Advance(Phase.Hold);
                }
                break;
            }

            case Phase.Hold:
            {
                // the crunch: compresses back in on contact and springs back
                // out to the full stretch, riding on top of it rather than
                // just sitting frozen at max extension
                float t = Progress(HoldSeconds);
                float recoil = MathF.Sin(MathHelper.Clamp(t / RecoilFraction, 0f, 1f) * MathF.PI) * RecoilAmount;
                _scaleX = StrikeScaleX * (1f - recoil);
                _scaleY = StrikeScaleY * (1f + recoil * 0.6f);
                if (_timer >= HoldSeconds) Advance(Phase.Retract);
                break;
            }

            case Phase.Retract:
            {
                float t = Progress(RetractSeconds);
                _scaleX = MathHelper.Lerp(StrikeScaleX, WindupScaleX, t);
                _scaleY = MathHelper.Lerp(StrikeScaleY, WindupScaleY, t);
                _bodyPos.X = MathHelper.Lerp(
                    PivotX(context, _side, GapHop) + DirSign * LungeDistance,
                    PivotX(context, _side, GapHop), t);
                // the danger's over — fade the fist back to plain white so it
                // doesn't linger colored once it can't hurt you any more
                _fistTint = Color.Lerp(FistBaseColor, Color.White, t);
                if (_timer >= RetractSeconds) Advance(Phase.Unsquish);
                break;
            }

            case Phase.Unsquish:
            {
                float t = Progress(UnsquishSeconds);
                _scaleX = MathHelper.Lerp(WindupScaleX, 1f, t);
                _scaleY = MathHelper.Lerp(WindupScaleY, 1f, t);
                if (_timer >= UnsquishSeconds)
                {
                    _scaleX = 1f; _scaleY = 1f;
                    // counted here, the moment a punch is actually finished
                    // recovering, not once the leap that follows it happens
                    // to land — the leap after the LAST punch doesn't land
                    // at all, see Launch
                    _punchesDone++;
                    Advance(Phase.Rest);
                }
                break;
            }

            case Phase.Rest:
                if (_timer >= RestSeconds)
                {
                    // decided now so Launch already knows where he's
                    // teleporting to once he's off screen
                    _landSide = Random.Shared.Next(2) == 0 ? Side.Left : Side.Right;
                    Advance(Phase.LeapCharge);
                }
                break;

            case Phase.LeapCharge:
            {
                // a real crouch, deeper than the punch wind up — this is
                // charging a jump, not coiling a hit
                float t = Progress(LeapChargeSeconds);
                _scaleX = MathHelper.Lerp(1f, LeapChargeScaleX, t);
                _scaleY = MathHelper.Lerp(1f, LeapChargeScaleY, t);
                if (_timer >= LeapChargeSeconds)
                {
                    SoundManager.Play(JumpSound);
                    Advance(Phase.Launch);
                }
                break;
            }

            case Phase.Launch:
            {
                // straight up and off the top of the screen, accelerating
                // like an actual launch rather than a lazy float. X doesn't
                // move here — he goes up from right where he's standing
                float t = Progress(LaunchSeconds);
                float ease = t * t;
                _bodyPos.Y = MathHelper.Lerp(FeetY(context), OffscreenY, ease);
                float stretchT = MathF.Min(t * 2f, 1f);
                _scaleX = MathHelper.Lerp(LeapChargeScaleX, LaunchStretchX, stretchT);
                _scaleY = MathHelper.Lerp(LeapChargeScaleY, LaunchStretchY, stretchT);

                if (_timer >= LaunchSeconds)
                {
                    // that was the last one — he's leaving for good, so the
                    // exit is the leap itself, not a leap that happens to be
                    // followed by nothing. no landing, no extra punch that
                    // was never coming
                    if (_punchesDone >= _punchCount)
                    {
                        _visible = false;
                        Advance(Phase.Done);
                        break;
                    }

                    // fully off screen now — free to reposition and flip for
                    // the new side with nobody able to see the snap happen
                    _side = _landSide;
                    _bodyPos.X = PivotX(context, _side, GapIdle);
                    _bodyPos.Y = OffscreenY;
                    Advance(Phase.Fall);
                }
                break;
            }

            case Phase.Fall:
            {
                // reappears above frame and drops back in — the same shape
                // as Cloverbyte's own entrance, just reused for the exit
                float t = Progress(FallSeconds);
                float ease = t * t;
                _bodyPos.Y = MathHelper.Lerp(OffscreenY, FeetY(context), ease);
                _scaleX = FallStretchX;
                _scaleY = FallStretchY;

                if (_timer >= FallSeconds)
                {
                    _bodyPos.Y = FeetY(context);
                    SoundManager.Play(LandSound);
                    Shake(context, 6f, 0.2f);
                    SpawnLandingSmoke(context);
                    Advance(Phase.LandSquash);
                }
                break;
            }

            case Phase.LandSquash:
            {
                // down hard, then settle — same squash-and-recover shape as
                // the wind up, just deeper, there's a lot more fall to stop
                float t = Progress(LandSquashSeconds);
                if (t < 0.4f)
                {
                    float dt2 = t / 0.4f;
                    _scaleX = MathHelper.Lerp(FallStretchX, LandSquashX, dt2);
                    _scaleY = MathHelper.Lerp(FallStretchY, LandSquashY, dt2);
                }
                else
                {
                    float rt = (t - 0.4f) / 0.6f;
                    _scaleX = MathHelper.Lerp(LandSquashX, 1f, rt);
                    _scaleY = MathHelper.Lerp(LandSquashY, 1f, rt);
                }

                if (_timer >= LandSquashSeconds)
                {
                    // reaching a landing at all means there's another punch
                    // due — the final leap skips straight from Launch to
                    // Done and never falls back in, see there
                    _scaleX = 1f; _scaleY = 1f;
                    Advance(Phase.Hop);
                }
                break;
            }
        }

        ApplyDamage(context);
    }

    // how far above the top of the window he has to go before he's fully
    // clear of it — generous on purpose so the launch stretch can't poke
    // back into frame
    private float OffscreenY => -(_rawHeight * 1.6f) - 60f;

    // ── damage ───────────────────────────────────────────────────────────────

    // he's only a hazard while he's actually reaching over the box — the
    // wind up and the telegraph are the warning, retract is the follow
    // through, neither one is the hit itself
    private bool IsDamagingPhase => _phase is Phase.Strike or Phase.Hold;

    private void ApplyDamage(DodgeContext context)
    {
        if (!_ready || !IsDamagingPhase) return;
        if (!TouchesPlayer(context)) return;
        if (!_fistColor.Connects(context.IsPlayerMoving)) return;
        context.DamagePlayer(PunchDamage);
    }

    private bool TouchesPlayer(DodgeContext context)
    {
        Rectangle hazard = HazardBounds();
        int half = GameSettings.DodgeHitboxSize / 2;
        hazard.Inflate(half, half);
        return hazard.Contains(context.HitboxPosition.ToPoint());
    }

    // conservative axis aligned envelope of his current silhouette: scale the
    // raw reach by the current squash/stretch, swing it by the current
    // rotation and take the box around that. cheap, and good enough for "did
    // any part of him touch you" rather than a pixel perfect read of the art
    private Rectangle HazardBounds()
    {
        float reachSide = _halfWidth * _scaleX;
        float reachUp   = _rawHeight * _scaleY;

        Span<Vector2> corners = stackalloc Vector2[]
        {
            new Vector2(-reachSide, -reachUp), new Vector2(reachSide, -reachUp),
            new Vector2(-reachSide, 0f),        new Vector2(reachSide, 0f),
        };

        float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
        foreach (Vector2 c in corners)
        {
            Vector2 p = _bodyPos + Rotate(c, _rotation);
            minX = MathF.Min(minX, p.X); maxX = MathF.Max(maxX, p.X);
            minY = MathF.Min(minY, p.Y); maxY = MathF.Max(maxY, p.Y);
        }

        return new Rectangle((int)minX, (int)minY, (int)(maxX - minX), (int)(maxY - minY));
    }

    // ── drawing ──────────────────────────────────────────────────────────────

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel, SpriteFont font)
    {
        if (!_ready || !_visible) return;

        SpriteEffects fx = Flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        float mirror = Flip ? -1f : 1f;

        for (int i = 0; i < _rig.Parts.Count; i++)
        {
            TeacherPartData part = _rig.Parts[i];
            Vector2 local = new Vector2(_rawDelta[i].X * _scaleX * mirror, _rawDelta[i].Y * _scaleY);
            Vector2 center = _bodyPos + Rotate(local, _rotation);

            Rectangle src = part.Src;
            var origin = new Vector2(src.Width / 2f, src.Height / 2f);
            var scale  = new Vector2(_scaleX * _rig.Scale, _scaleY * _rig.Scale);
            Color tint = i == _nearArmIndex ? _fistTint : _bodyTint;

            spriteBatch.Draw(_texture, center, src, tint, _rotation, origin, scale, fx, 0f);
        }

        if (font != null && ShowCurveLabel) DrawCurveLabel(spriteBatch, font);
    }

    // the callout — a plain caption above his head naming the curve the
    // reach just followed, not a speech bubble. the normal bubble anchors to
    // the passive floating idle, which this pattern deliberately never shows
    private void DrawCurveLabel(SpriteBatch spriteBatch, SpriteFont font)
    {
        string text = CurveLabel;
        // fades out on the same schedule as the fist tint, rather than
        // popping off the instant Retract hands off to Unsquish
        float alpha = _phase == Phase.Retract ? 1f - Progress(RetractSeconds) : 1f;

        Vector2 size = font.MeasureString(text);
        Vector2 pos = new Vector2(
            _bodyPos.X - size.X / 2f,
            _bodyPos.Y - _rawHeight * _scaleY - size.Y - 6f);

        spriteBatch.DrawString(font, text, pos, Color.White * alpha);
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private void Advance(Phase next)
    {
        _phase = next;
        _timer = 0f;
    }

    // reads the teacher's part rects/offsets once and works out the pivot
    // (his feet — bottom-center of the assembled rig) and every part's
    // position relative to it, in raw sprite-sheet pixels. everything the
    // pattern animates is a transform of these, so retuning his offsets in
    // the JSON carries straight through without touching this file
    private void BuildRig()
    {
        float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
        foreach (TeacherPartData p in _rig.Parts)
        {
            minX = MathF.Min(minX, p.Offset.X);
            minY = MathF.Min(minY, p.Offset.Y);
            maxX = MathF.Max(maxX, p.Offset.X + p.Src.Width);
            maxY = MathF.Max(maxY, p.Offset.Y + p.Src.Height);
        }

        _rawPivot = new Vector2((minX + maxX) / 2f, maxY);

        _rawDelta = new Vector2[_rig.Parts.Count];
        for (int i = 0; i < _rig.Parts.Count; i++)
        {
            TeacherPartData p = _rig.Parts[i];
            Vector2 center = p.Offset + new Vector2(p.Src.Width / 2f, p.Src.Height / 2f);
            _rawDelta[i] = center - _rawPivot;
        }

        _halfWidth = (maxX - minX) * _rig.Scale / 2f;
        _rawHeight = (maxY - minY) * _rig.Scale;

        for (int i = 0; i < _rig.Parts.Count; i++)
            if (_rig.Parts[i].Name == "nearArm") { _nearArmIndex = i; break; }
    }

    // a part's current on screen center, transformed the same way Draw
    // renders it — used to anchor the impact burst on the fist rather than
    // some generic corner of the bounding box
    private Vector2 CurrentPartCenter(int index)
    {
        float mirror = Flip ? -1f : 1f;
        Vector2 local = new Vector2(_rawDelta[index].X * _scaleX * mirror, _rawDelta[index].Y * _scaleY);
        return _bodyPos + Rotate(local, _rotation);
    }

    // dust kicked outward from the fist the instant the strike lands, thrown
    // along the punch direction so it agrees with where the hit came from
    private void SpawnImpactBurst(DodgeContext context)
    {
        Spritesheet smoke = SpriteManager.GetSprite("smoke");
        Texture2D tex = smoke?.Texture;

        Vector2 origin = _nearArmIndex >= 0 ? CurrentPartCenter(_nearArmIndex) : _bodyPos;
        float baseAngle = DirSign > 0f ? 0f : MathF.PI;

        for (int i = 0; i < 16; i++)
        {
            float spread = ((float)Random.Shared.NextDouble() - 0.5f) * MathHelper.ToRadians(150f);
            float angle  = baseAngle + spread;
            float speed  = 90f + (float)Random.Shared.NextDouble() * 130f;
            var vel = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * speed;
            context.SpawnParticle(origin, vel, 0.45f, 8 + Random.Shared.Next(10), Color.White, 0.05f, tex);
        }
    }

    // smoke kicked up from his feet on landing, pushed outward from center so
    // it reads as displaced by the impact rather than just appearing
    private void SpawnLandingSmoke(DodgeContext context)
    {
        Spritesheet smoke = SpriteManager.GetSprite("smoke");
        Texture2D tex = smoke?.Texture;

        for (int i = 0; i < 12; i++)
        {
            var pos = new Vector2(
                _bodyPos.X + ((float)Random.Shared.NextDouble() * 2f - 1f) * _halfWidth,
                _bodyPos.Y - (float)Random.Shared.NextDouble() * 6f);

            float dir = pos.X < _bodyPos.X ? -1f : 1f;
            var vel = new Vector2(
                dir * (25f + (float)Random.Shared.NextDouble() * 45f),
                -20f - (float)Random.Shared.NextDouble() * 35f);

            context.SpawnParticle(pos, vel, 0.7f, 10 + Random.Shared.Next(10), Color.White, 0.15f, tex);
        }
    }

    private float FeetY(DodgeContext context) => context.BaseBox.Bottom;

    private float PivotX(DodgeContext context, Side side, float gap)
    {
        Rectangle box = context.BaseBox;
        return side == Side.Left
            ? box.Left  - gap - _halfWidth
            : box.Right + gap + _halfWidth;
    }

    // every shake in this attack goes through here so the rattle and the
    // sound can't drift apart when the timings get retuned
    private static void Shake(DodgeContext context, float magnitude, float seconds)
    {
        context.ShakeScreen(magnitude, seconds);
        SoundManager.Play(ShakeSound);
    }

    private float Progress(float seconds) => MathHelper.Clamp(_timer / seconds, 0f, 1f);

    private static float EaseOut(float t) => 1f - MathF.Pow(1f - t, 3f);
    private static float EaseIn(float t)  => t * t * t;

    // which curve the strike's reach follows, keyed off how many punches are
    // already done — first is a flat lerp, then one of each easing kind
    private float Ease(float t) => _punchesDone switch
    {
        0 => t,
        1 => EaseIn(t),
        _ => EaseOut(t),
    };

    private string CurveLabel => _punchesDone switch
    {
        0 => "LINEAR!",
        1 => "EASE IN!",
        _ => "EASE OUT!",
    };

    // up through Strike/Hold so it's still readable while he's actually
    // dangerous, fading with the fist color during Retract
    private bool ShowCurveLabel => _phase is Phase.Telegraph or Phase.Strike or Phase.Hold or Phase.Retract;

    private static Vector2 Rotate(Vector2 v, float radians)
    {
        float cos = MathF.Cos(radians), sin = MathF.Sin(radians);
        return new Vector2(v.X * cos - v.Y * sin, v.X * sin + v.Y * cos);
    }
}
