using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FinalProject.Core;
using FinalProject.Core.Audio;
using FinalProject.Core.Graphics;

namespace FinalProject.Battle.Patterns;

// Dor's cloverbyte attack. The camera pans so he and the arena slide left,
// leaving room on the right for the cloverbyte to drop in. It lands, squashes
// once, then lashes its tongue at wherever the soul is, five times.
//
// What follows depends on how many times he's already thrown it (see Tier).
// The first two are lessons: a sixth lash feints under the box, turns blue and
// sweeps the arena, and the second time an orange sweep comes straight back
// down through it. From the third on he drops the lesson entirely — the
// scripted finale is gone and colour sweeps are woven between the lashes
// instead, each one opening as an ordinary lash before recolouring at random
// and swinging either way up the arc.
//
// The sprite itself is never animated, it's one static frame the whole way
// through. Everything that moves is the tongue, plus a squash on landing and a
// few px of recoil on each strike.
//
// BLUE ATTACK: the sweep only hurts you while you're holding a direction, the
// same rule undertale uses. See DodgeContext.IsPlayerMoving.
//
// Draws itself (logo + tongue), DodgePhase just forwards the call.
public class CloverbytePattern : IBulletPattern
{
    // worked out in PlanBeats once the coin flips are known. it has to be fixed
    // before the turn starts, so the layout is rolled up front rather than as
    // it goes — otherwise an unlucky run gets cut off and a lucky one sits idle
    public float Duration => _duration;

    // he only runs the lesson twice, then assumes you've got it:
    //   1st   feint, then a blue sweep            — here's what blue means
    //   2nd   the same, plus an orange one back   — and here's orange
    //   3rd+  no lesson and no feint. the colour sweeps are woven between the
    //         lashes instead, colour and direction both picked the instant the
    //         lash lands so there's nothing to read ahead of time
    // static because a fresh pattern instance is built every turn — BattleState
    // clears it on OnEnter so the count can't carry between fights
    private enum Tier { TeachBlue, TeachOrange, Randomised }

    // he splits his turns with the boat, so this only comes up every other turn
    private const int  TeachOrangeAt = 2;
    private const int  RandomiseAt   = 3;
    private static int _timesUsed;

    public static void ResetUseCount() => _timesUsed = 0;

    // ── timing ───────────────────────────────────────────────────────────────
    private const float PanSeconds     = 1.1f;
    private const float FallSeconds    = 0.7f;
    private const float SquishSeconds  = 0.45f;
    private const float AimSeconds     = 0.70f; // tracks the soul, this is your warning
    private const float StrikeSeconds  = 0.18f; // target's already locked by now
    private const float HoldSeconds    = 0.14f;
    private const float RetractSeconds = 0.28f;
    private const float RestSeconds    = 0.50f; // beat between lashes, tongue fully in
    private const float FeintAimSeconds    = 0.80f;
    private const float FeintStrikeSeconds = 0.18f;
    private const float WindUpSeconds      = 0.90f; // blue, telegraphs the sweep
    private const float SwipeSeconds       = 0.38f; // fast on purpose, it's a whip crack
    private const float ImpactSeconds      = 0.45f; // screen rattles, trail hangs then fades
    private const float ReturnWindUpSeconds = 0.50f; // shorter — it's a punish, not a warning
    private const float RecolourSeconds     = 0.35f; // tier 3: all the read you get
    private const float RecoverSeconds     = 1.10f;

    private const int LashCount   = 5; // plain lashes, tiers 1 and 2
    private const int LashCountT3 = 8; // tier 3 throws noticeably more

    // tier 3: odds that any given gap between two lashes gets a colour sweep.
    // rolled per gap, so two can land back to back and the layout never repeats
    private const float SwipeChance   = 0.5f;
    private const float DurationSlack = 0.4f;

    // what one beat costs, so Duration can be worked out instead of guessed
    private const float OpeningSeconds   = PanSeconds + FallSeconds + SquishSeconds;
    private const float LashBeatSeconds  = AimSeconds + StrikeSeconds + HoldSeconds
                                         + RetractSeconds + RestSeconds;
    private const float SweepBeatSeconds = AimSeconds + StrikeSeconds + HoldSeconds
                                         + RecolourSeconds + SwipeSeconds + ImpactSeconds
                                         + RetractSeconds + RestSeconds;
    private const float FinaleSeconds    = FeintAimSeconds + FeintStrikeSeconds
                                         + WindUpSeconds + SwipeSeconds + ImpactSeconds;
    private const float ReturnSeconds    = ReturnWindUpSeconds + SwipeSeconds + ImpactSeconds;

    // ── layout ───────────────────────────────────────────────────────────────
    // + moves the view right, so the box and Dor appear to slide left
    private const float CameraPanX = 200f;

    // drawn 1:1, scaling pixel art to a non integer size is what made the first
    // version of this look mushy
    private const int LogoW = 128;
    private const int LogoH = 134;

    // px from the right edge of the arena to the logo's left edge. big on
    // purpose, the gap is the runway the tongue crosses
    private const int LogoGap = 210;

    // where the tongue comes out of, measured off the sprite in paint.net.
    // px from the logo's top left, not a fraction, so it stays put if the
    // sprite is ever swapped for one a different size
    private const int MouthPx = 53;
    private const int MouthPy = 70;

    // ── look ─────────────────────────────────────────────────────────────────
    private const float SquashAmount   = 0.72f;
    private const float LandShake      = 7f;
    private const float RecoilPx       = 5f;  // logo kicks back as the tongue fires
    private const float TongueWidth    = 6f;
    private const float SwipeWidth     = 18f; // the sweep is much fatter
    private const float ImpactShake    = 16f; // the bang at the end of the sweep
    private const float SwipeRadius    = 480f; // long enough to clear the far side
    private const float SwipeArcDeg    = 85f;  // sweeps up through straight-left
    private const float WindUpPulseHz  = 6f;

    private static readonly Color TongueColor = Color.White;
    private static readonly Color BlueColor   = new Color(60, 130, 255);
    private static readonly Color OrangeColor = new Color(255, 150, 40);

    // undertale's rule: blue only connects while you're moving, orange only
    // while you're standing still, white always connects
    private enum HazardColor { White, Blue, Orange }

    private const int TongueDamage = 5;
    private const int SwipeDamage  = 7;

    private const string ThudSound       = "thud";
    private const string ShakeSound      = "snd_screenshake"; // rides along with every shake
    private const string SwipeSound      = "snd_heavydamage"; // the sweep itself
    private const string TongueRiseSound = "snd_spearrise";   // tongue coming out, the telegraph
    private const string StrikeSound     = "snd_grab";        // the lash landing

    private enum Phase
    {
        Pan, Fall, Squish,
        Aim, Strike, Hold, Recolour, Retract, Rest,
        FeintAim, FeintStrike,
        WindUp, Swipe, Impact,
        ReturnWindUp, SwipeBack,
        Recover, Done
    }

    private Phase _phase = Phase.Pan;
    private float _timer;
    private int   _lashesDone;

    // logo placement, fixed once at Start — it does not follow the box
    private int     _logoLeft;
    private int     _logoTop;
    private float   _fallFromY;

    // the mouth, where the tongue is anchored. computed rather than stored so
    // it can't go stale, and so it carries the recoil — the tongue has to stay
    // attached to the sprite when the body kicks back
    private Vector2 Pivot => new(_logoLeft + MouthPx + _recoil, _logoTop + MouthPy);

    private float _squash = 1f;
    private float _recoil;       // px the logo is pushed right by a strike

    // tongue state. tip = pivot + dir(_angle) * _length * _extend
    private float _angle;
    private float _prevAngle;    // last frame's angle, so the sweep can't tunnel
    private float _length;
    private float _extend;       // 0 retracted, 1 fully out
    private HazardColor _color = HazardColor.White;
    private float _swipeStartAngle;
    private float _trailFromAngle;         // where the swept wedge is drawn from
    private float _trailAlpha = 1f;        // wedge hangs after the swipe, then fades
    private float _sweepDir = 1f;          // +1 sweeps up the arc, -1 sweeps down
    private float _lashLength;             // reach of the lash a sweep grew out of
    private Tier  _tier;                   // locked in at Start so Duration is stable
    private bool  _swipeLash;              // this beat ends in a colour sweep
    private bool  _returnSwipeDone;
    private float _duration;
    private int   _lashTarget;

    // one entry per gap between lashes: does a colour sweep go there. rolled in
    // PlanBeats and consumed as it goes, so a gap can't fire twice
    private readonly List<bool> _sweepAfter = new();

    private bool _visible;

    public void Start(DodgeContext context)
    {
        Rectangle box = context.BaseBox;

        _logoLeft  = box.Right + LogoGap;
        _logoTop   = box.Center.Y - LogoH / 2;
        _fallFromY = -LogoH - 40f; // above the top of the screen

        _timesUsed++;
        _tier = _timesUsed >= RandomiseAt   ? Tier.Randomised
              : _timesUsed >= TeachOrangeAt ? Tier.TeachOrange
              :                               Tier.TeachBlue;

        _lashTarget = _tier == Tier.Randomised ? LashCountT3 : LashCount;
        PlanBeats();

        // he watches this one happen, so put him on screen and let him talk
        context.SetTeacherVisible(true);
        context.SetTeacherSpeech(_tier switch
        {
            Tier.Randomised  => "You know both rules now. Keep up.",
            Tier.TeachOrange => "You've seen this one already. Let's add a step.",
            _                => "Let me show you what we ship at Cloverbyte.",
        });
    }

    public void Update(GameTime gameTime, DodgeContext context)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _timer += dt;

        // recoil always eases back to rest wherever we are
        _recoil = MathHelper.Lerp(_recoil, 0f, MathHelper.Clamp(dt * 12f, 0f, 1f));

        switch (_phase)
        {
            case Phase.Pan:
                context.SetCameraPan(new Vector2(
                    MathHelper.SmoothStep(0f, CameraPanX, Progress(PanSeconds)), 0f));
                if (_timer >= PanSeconds) Advance(Phase.Fall);
                break;

            case Phase.Fall:
                _visible = true;
                // eases in, so it reads as dropping rather than sliding
                float ft = Progress(FallSeconds);
                _logoTop = (int)MathHelper.Lerp(_fallFromY, LandTop(context), ft * ft);
                if (ft >= 1f)
                {
                    _logoTop = LandTop(context);
                    SoundManager.Play(ThudSound);
                    Shake(context, LandShake, 0.3f);
                    SpawnLandingSmoke(context);
                    Advance(Phase.Squish);
                }
                break;

            case Phase.Squish:
                // down then back, the only deformation the sprite ever gets
                float st = Progress(SquishSeconds);
                _squash = st < 0.45f
                    ? MathHelper.Lerp(1f, SquashAmount, st / 0.45f)
                    : MathHelper.Lerp(SquashAmount, 1f, (st - 0.45f) / 0.55f);
                if (st >= 1f)
                {
                    _squash = 1f;
                    SoundManager.Play(TongueRiseSound);
                    Advance(Phase.Aim);
                }
                break;

            case Phase.Aim:
                // follows the soul the whole wind up, then commits to wherever
                // it was on the last frame. that's the tell — move after this.
                // always opens white: a colour sweep starts life as an ordinary
                // lash so there's nothing to read until it has already landed
                _color = HazardColor.White;
                AimAt(context.HitboxPosition);
                _extend = 0f;
                if (_timer >= AimSeconds)
                {
                    _recoil = RecoilPx;
                    SoundManager.Play(StrikeSound);
                    Advance(Phase.Strike);
                }
                break;

            case Phase.Strike:
                _extend = Progress(StrikeSeconds);
                if (_timer >= StrikeSeconds) { _extend = 1f; Advance(Phase.Hold); }
                break;

            case Phase.Hold:
                _extend = 1f;
                if (_timer >= HoldSeconds)
                {
                    if (_swipeLash) BeginColourSweep();
                    else            Advance(Phase.Retract);
                }
                break;

            case Phase.Recolour:
                // still buried in the soul's last position, but coloured now and
                // stretching out to full sweep reach. this is the whole read, and
                // it's live the entire time — blue here already wants you frozen,
                // before it has swung anywhere
                _extend = 1f;
                _length = MathHelper.Lerp(_lashLength, SwipeRadius, Progress(RecolourSeconds));
                if (_timer >= RecolourSeconds)
                {
                    _length          = SwipeRadius;
                    _swipeStartAngle = _angle;
                    _trailFromAngle  = _angle;
                    SoundManager.Play(SwipeSound);
                    Shake(context, 9f, 0.2f);
                    Advance(Phase.Swipe);
                }
                break;

            case Phase.Retract:
                _extend = 1f - Progress(RetractSeconds);
                if (_timer >= RetractSeconds)
                {
                    _extend = 0f;
                    // woven sweeps sit between the lashes, they aren't one of them
                    if (_swipeLash) _swipeLash = false;
                    else            _lashesDone++;
                    Advance(Phase.Rest);
                }
                break;

            case Phase.Rest:
                // nothing on screen but the logo. gives you room to reposition
                // and keeps the five lashes from blurring into one long flail
                _extend = 0f;
                if (_timer >= RestSeconds)
                {
                    if (_lashesDone >= _lashTarget)
                    {
                        // tier 3 has no finale — once the lashes are spent it's over
                        if (_tier == Tier.Randomised) { Advance(Phase.Recover); break; }

                        SoundManager.Play(TongueRiseSound);
                        context.SetTeacherSpeech("Enough warm up.");
                        Advance(Phase.FeintAim);
                        break;
                    }

                    // take the sweep planned for this gap, if there is one, and
                    // clear it so coming back round can't fire the same gap twice
                    int gap = _lashesDone - 1;
                    _swipeLash = gap >= 0 && gap < _sweepAfter.Count && _sweepAfter[gap];
                    if (_swipeLash) _sweepAfter[gap] = false;

                    // same telegraph either way — a sweep has to open looking
                    // exactly like a plain lash
                    SoundManager.Play(TongueRiseSound);
                    Advance(Phase.Aim);
                }
                break;

            case Phase.FeintAim:
                // deliberately lines up under the arena instead of on the soul
                AimAt(FeintTarget(context));
                _extend = 0f;
                if (_timer >= FeintAimSeconds)
                {
                    _recoil = RecoilPx;
                    SoundManager.Play(StrikeSound);
                    Advance(Phase.FeintStrike);
                }
                break;

            case Phase.FeintStrike:
                _extend = Progress(FeintStrikeSeconds);
                if (_timer >= FeintStrikeSeconds)
                {
                    _extend          = 1f;
                    _color           = HazardColor.Blue;
                    _length          = SwipeRadius;
                    _swipeStartAngle = _angle;
                    _trailFromAngle  = _angle;
                    context.SetTeacherSpeech("BLUE means you hold still. Were you not listening?");
                    Advance(Phase.WindUp);
                }
                break;

            case Phase.WindUp:
                // parked under the box, blue, pulsing. this is the read
                _extend = 1f;
                if (_timer >= WindUpSeconds)
                {
                    SoundManager.Play(SwipeSound);
                    Shake(context, 9f, 0.2f);
                    Advance(Phase.Swipe);
                }
                break;

            case Phase.Swipe:
                // eases OUT, not in and out. it leaves the mark at full speed and
                // follows through, a symmetric ease made it creep off and creep
                // back in, which is what killed the impact
                _angle = _swipeStartAngle
                       + MathHelper.ToRadians(SwipeArcDeg) * _sweepDir * EaseOut(Progress(SwipeSeconds));
                if (_timer >= SwipeSeconds)
                {
                    _angle = _swipeStartAngle + MathHelper.ToRadians(SwipeArcDeg) * _sweepDir;
                    SoundManager.Play(ThudSound);
                    Shake(context, ImpactShake, ImpactSeconds);
                    _recoil = RecoilPx * 2.5f;
                    SpawnSwipeDebris(context);
                    Advance(Phase.Impact);
                }
                break;

            case Phase.Impact:
                // holds the end of the arc while the screen rattles. the trail
                // fades out over the same beat so it doesn't just blink away
                _extend     = 1f;
                _trailAlpha = 1f - Progress(ImpactSeconds);
                if (_timer >= ImpactSeconds)
                {
                    // every sweep lands here, so this is the fork back out
                    if (_tier == Tier.TeachOrange && !_returnSwipeDone)
                    {
                        _color = HazardColor.Orange;
                        context.SetTeacherSpeech("ORANGE. Now MOVE.");
                        Advance(Phase.ReturnWindUp);
                    }
                    else if (_tier == Tier.Randomised)
                    {
                        // woven sweep is spent, fall back into the lash rhythm
                        Advance(Phase.Retract);
                    }
                    else Advance(Phase.Recover);
                }
                break;

            case Phase.ReturnWindUp:
                // parked at the top of the arc, orange now. short on purpose —
                // standing still is what just saved you, so the punish for
                // keeping still has to arrive before you get comfortable
                _extend     = 1f;
                _trailAlpha = 1f;
                if (_timer >= ReturnWindUpSeconds)
                {
                    _trailFromAngle = _angle;
                    SoundManager.Play(SwipeSound);
                    Shake(context, 9f, 0.2f);
                    Advance(Phase.SwipeBack);
                }
                break;

            case Phase.SwipeBack:
                // same arc, travelled back down to where it started
                _angle = _swipeStartAngle
                       + MathHelper.ToRadians(SwipeArcDeg) * _sweepDir * (1f - EaseOut(Progress(SwipeSeconds)));
                if (_timer >= SwipeSeconds)
                {
                    _angle = _swipeStartAngle;
                    _returnSwipeDone = true;
                    SoundManager.Play(ThudSound);
                    Shake(context, ImpactShake, ImpactSeconds);
                    _recoil = RecoilPx * 2.5f;
                    SpawnSwipeDebris(context);
                    Advance(Phase.Impact);
                }
                break;

            case Phase.Recover:
                _extend = 1f - Progress(RecoverSeconds * 0.4f);
                if (_extend < 0f) _extend = 0f;
                context.SetCameraPan(new Vector2(
                    MathHelper.SmoothStep(CameraPanX, 0f, Progress(RecoverSeconds)), 0f));
                if (_timer >= RecoverSeconds)
                {
                    context.SetCameraPan(Vector2.Zero);
                    _visible = false;
                    Advance(Phase.Done);
                }
                break;
        }

        ApplyDamage(context);

        // after the damage check, so next frame knows where the arc started.
        // Aim moves this around a lot while it tracks the soul, but the tongue
        // is retracted then so nothing gets swept
        _prevAngle = _angle;
    }

    // ── damage ───────────────────────────────────────────────────────────────

    private void ApplyDamage(DodgeContext context)
    {
        if (_extend <= 0.01f) return;
        if (!IsDamagingPhase) return;
        if (!Connects(_color, context.IsPlayerMoving)) return;

        bool sweep  = _color != HazardColor.White;
        float reach = (sweep ? SwipeWidth : TongueWidth) / 2f + GameSettings.DodgeHitboxSize / 2f;

        // Testing only the angle the tongue is at RIGHT NOW misses the sweeps
        // entirely. EaseOut opens at 3x the average rate, ~670 deg/sec, which at
        // arm's length is ~70px of travel in a single frame — the line steps
        // clean over a 26px hit window and never overlaps on any sampled frame.
        // So walk the arc it covered since last frame instead of sampling a
        // point on it. Static phases come out as delta 0 and one sample.
        float reachOut = _length * _extend;
        float delta    = _angle - _prevAngle;

        // one sample per ~8px of tip travel, so the step is always well inside
        // the hit window no matter how far out the tip is
        int steps = (int)MathF.Ceiling(MathF.Abs(delta) * reachOut / 8f);
        steps = Math.Clamp(steps, 1, 64);

        for (int i = 0; i <= steps; i++)
        {
            float a = _prevAngle + delta * (i / (float)steps);
            Vector2 tip = Pivot + new Vector2(MathF.Cos(a), MathF.Sin(a)) * reachOut;

            if (DistToSegment(context.HitboxPosition, Pivot, tip) <= reach)
            {
                context.DamagePlayer(sweep ? SwipeDamage : TongueDamage);
                return;
            }
        }
    }

    private bool IsDamagingPhase => _phase is Phase.Strike or Phase.Hold
        or Phase.Recolour or Phase.WindUp or Phase.Swipe or Phase.Impact
        or Phase.ReturnWindUp or Phase.SwipeBack;

    // the sweep passes right over you either way, the colour decides whether it
    // actually connects. blue wants you still, orange wants you moving
    private static bool Connects(HazardColor color, bool moving) => color switch
    {
        HazardColor.Blue   => moving,
        HazardColor.Orange => !moving,
        _                  => true,
    };

    // ── drawing ──────────────────────────────────────────────────────────────

    public void Draw(SpriteBatch sb, Texture2D pixel)
    {
        if (!_visible) return;

        // logo first — the tongue comes out of the middle of the sprite, so it
        // has to sit on top of him or it looks like it's coming from behind
        DrawLogo(sb);

        // the swept area trails behind the leading edge so the sweep reads as a
        // single arc rather than a line that teleported
        if (_phase is Phase.Swipe or Phase.SwipeBack or Phase.Impact)
            DrawSweptWedge(sb, pixel);

        if (_extend > 0.01f) DrawTongue(sb, pixel);
    }

    private void DrawLogo(SpriteBatch sb)
    {
        Spritesheet sheet = SpriteManager.GetSprite("cloverbyte");
        if (sheet?.Texture == null) return;

        // squash is anchored to the feet so it flattens down into the ground,
        // and widens a touch to keep the volume looking right
        int h = (int)(LogoH * _squash);
        int w = (int)(LogoW * (1f + (1f - _squash) * 0.35f));
        int bottom = _logoTop + LogoH;

        var dst = new Rectangle(
            _logoLeft + (LogoW - w) / 2 + (int)_recoil,
            bottom - h, w, h);

        sb.Draw(sheet.Texture, dst, Color.White);
    }

    private void DrawTongue(SpriteBatch sb, Texture2D pixel)
    {
        Color color = TongueColor;
        float width = TongueWidth;

        if (_color != HazardColor.White)
        {
            width = SwipeWidth;
            color = SweepColor;

            // pulses while it's parked, so it's obvious something is coming and
            // that it isn't the same colour as the five lashes before it
            if (_phase is Phase.WindUp or Phase.ReturnWindUp)
            {
                float pulse = (MathF.Sin(_timer * WindUpPulseHz * MathHelper.TwoPi) + 1f) * 0.5f;
                color = Color.Lerp(SweepColor, Color.White, pulse * 0.5f);
            }
            // the leading edge runs hot while it's actually swinging
            else if (_phase is Phase.Swipe or Phase.SwipeBack or Phase.Impact)
            {
                color = Color.Lerp(SweepColor, Color.White, 0.4f) * MathHelper.Max(_trailAlpha, 0.35f);
            }
        }

        DrawLine(sb, pixel, Pivot, Tip(), width, color);
    }

    private void DrawSweptWedge(SpriteBatch sb, Texture2D pixel)
    {
        // signed, the return sweep runs the other way round the arc
        float swept = _angle - _trailFromAngle;
        int steps = Math.Max(1, (int)(MathF.Abs(MathHelper.ToDegrees(swept)) / 2f));

        for (int i = 0; i < steps; i++)
        {
            float t = i / (float)steps;              // 0 = oldest, 1 = leading edge
            float a = _trailFromAngle + swept * t;
            var end = Pivot + new Vector2(MathF.Cos(a), MathF.Sin(a)) * _length * _extend;

            // fades out the further behind the leading edge it is
            DrawLine(sb, pixel, Pivot, end, SwipeWidth,
                SweepColor * ((0.14f + t * 0.5f) * _trailAlpha));
        }
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private void Advance(Phase next)
    {
        _phase = next;
        _timer = 0f;
    }

    // rolls the whole layout before the attack starts and totals up what it will
    // cost, so Duration is exact no matter how the coin flips land
    private void PlanBeats()
    {
        float seconds = OpeningSeconds + RecoverSeconds + _lashTarget * LashBeatSeconds;

        if (_tier == Tier.Randomised)
        {
            // one flip per gap between lashes — none after the last, tier 3
            // deliberately doesn't end on a sweep
            for (int i = 0; i < _lashTarget - 1; i++)
            {
                bool sweep = Random.Shared.NextDouble() < SwipeChance;
                _sweepAfter.Add(sweep);
                if (sweep) seconds += SweepBeatSeconds;
            }
        }
        else
        {
            seconds += FinaleSeconds;
            if (_tier == Tier.TeachOrange) seconds += ReturnSeconds;
        }

        _duration = seconds + DurationSlack;
    }

    // the lash has already landed by the time this runs. picking the colour and
    // the direction here, at the last possible moment, is what stops the wind up
    // or the lash itself leaking which one is coming
    private void BeginColourSweep()
    {
        _color      = Random.Shared.Next(2) == 0 ? HazardColor.Blue : HazardColor.Orange;
        _sweepDir   = Random.Shared.Next(2) == 0 ? 1f : -1f;
        _lashLength = _length;
        _trailAlpha = 1f;
        Advance(Phase.Recolour);
    }

    // every shake in this attack goes through here so the rattle and the sound
    // can't drift apart when the timings get retuned
    private static void Shake(DodgeContext context, float magnitude, float seconds)
    {
        context.ShakeScreen(magnitude, seconds);
        SoundManager.Play(ShakeSound);
    }

    private float Progress(float seconds) => MathHelper.Clamp(_timer / seconds, 0f, 1f);

    // fast off the mark, decelerating into the follow through
    private static float EaseOut(float t) => 1f - MathF.Pow(1f - t, 3f);

    private Color SweepColor => _color == HazardColor.Orange ? OrangeColor : BlueColor;

    private int LandTop(DodgeContext context) => context.BaseBox.Center.Y - LogoH / 2;

    private Vector2 Tip()
        => Pivot + new Vector2(MathF.Cos(_angle), MathF.Sin(_angle)) * _length * _extend;

    private void AimAt(Vector2 target)
    {
        Vector2 d = target - Pivot;
        _angle  = MathF.Atan2(d.Y, d.X);
        _length = d.Length();
    }

    // under the arena, well clear of anywhere the soul can actually be
    private static Vector2 FeintTarget(DodgeContext context)
    {
        Rectangle box = context.BaseBox;
        return new Vector2(box.Center.X - 20, box.Bottom + 80);
    }

    private void SpawnLandingSmoke(DodgeContext context)
    {
        Spritesheet smoke = SpriteManager.GetSprite("smoke");
        Texture2D tex = smoke?.Texture; // null just draws plain squares

        float bottom = _logoTop + LogoH;
        for (int i = 0; i < 12; i++)
        {
            var pos = new Vector2(
                _logoLeft + (float)Random.Shared.NextDouble() * LogoW,
                bottom - 10 + (float)Random.Shared.NextDouble() * 16f);

            // pushed outward from the middle so it looks displaced by the impact
            float dir = pos.X < _logoLeft + LogoW / 2f ? -1f : 1f;
            var vel = new Vector2(
                dir * (25f + (float)Random.Shared.NextDouble() * 45f),
                -20f - (float)Random.Shared.NextDouble() * 35f);

            context.SpawnParticle(pos, vel, 0.8f, 12 + Random.Shared.Next(12),
                Color.White, 0.15f, tex);
        }
    }

    // dust kicked up across the arena the sweep just went through, thrown along
    // the direction it was travelling so the debris agrees with the swing
    private void SpawnSwipeDebris(DodgeContext context)
    {
        Spritesheet smoke = SpriteManager.GetSprite("smoke");
        Texture2D tex = smoke?.Texture;

        Rectangle box = context.BaseBox;
        var dir = new Vector2(MathF.Cos(_angle), MathF.Sin(_angle));

        for (int i = 0; i < 14; i++)
        {
            var pos = new Vector2(
                box.Left + (float)Random.Shared.NextDouble() * box.Width,
                box.Top  + (float)Random.Shared.NextDouble() * box.Height);

            float speed = 60f + (float)Random.Shared.NextDouble() * 90f;
            var spread = new Vector2(
                ((float)Random.Shared.NextDouble() * 2f - 1f) * 30f,
                ((float)Random.Shared.NextDouble() * 2f - 1f) * 30f);

            context.SpawnParticle(pos, dir * speed + spread, 0.6f,
                10 + Random.Shared.Next(10), Color.White, 0.05f, tex);
        }
    }

    private static float DistToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float lenSq = ab.LengthSquared();
        if (lenSq < 0.0001f) return Vector2.Distance(p, a);
        float t = MathHelper.Clamp(Vector2.Dot(p - a, ab) / lenSq, 0f, 1f);
        return Vector2.Distance(p, a + ab * t);
    }

    private static void DrawLine(SpriteBatch sb, Texture2D pixel, Vector2 a, Vector2 b,
                                 float thickness, Color color)
    {
        Vector2 delta = b - a;
        float length = delta.Length();
        if (length < 0.5f) return;

        sb.Draw(pixel, a, null, color, MathF.Atan2(delta.Y, delta.X),
            new Vector2(0f, 0.5f), new Vector2(length, thickness), SpriteEffects.None, 0f);
    }
}
