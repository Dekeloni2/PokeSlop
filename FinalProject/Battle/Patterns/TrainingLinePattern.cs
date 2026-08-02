using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FinalProject.Core;
using FinalProject.Core.Audio;

namespace FinalProject.Battle.Patterns;

// The training dummy's three lessons. Each one is a single line sweeping across
// a wide arena at the same speed, and each teaches exactly one rule:
//
//   White   a gap in the line. move so you're standing in the gap. this is
//           just "you can move, and contact hurts"
//   Blue    no gap, so it WILL pass through you. blue only connects while
//           you're moving, so the answer is to stop and let it go by
//   Orange  the same again, inverted. orange only connects while you're
//           standing still, so the answer is to keep walking through it
//
// Blue and orange are the whole reason this fight exists — they're unreadable
// if you've never played undertale, so the dummy states the rule outright in
// its bubble before each line moves, and says whether you got it afterwards.
// Real lines live in the teacher's JSON, these are only the fallbacks.
//
// The arena grows to the width of the menu box first (see context.MenuBox), so
// the line has a long runway and there's real time to react in.
//
// BattleState only advances the dummy to the next lesson on a clean pass, so
// getting hit repeats the same rule until it lands. That gating is in
// BattleState.UpdateDodging, not here — this pattern just reports hits through
// the usual DodgePhase counter.
//
// Draws itself, DodgePhase forwards the call.
public class TrainingLinePattern : IBulletPattern
{
    // which rule this line is teaching — see HazardRule. this pattern is where
    // the player meets it for the first time, one colour per lesson
    private readonly HazardRule _kind;

    public TrainingLinePattern(HazardRule kind) => _kind = kind;

    // ── timing ───────────────────────────────────────────────────────────────
    private const float ExpandSeconds  = 0.5f;
    // long enough to read a rule off the bubble before anything moves. this is
    // the tutorial's entire teaching window, so it's deliberately unhurried
    private const float WarnSeconds    = 1.9f;
    private const float RecoverSeconds = 1.2f;
    private const float DurationSlack  = 0.4f;

    // px/sec, shared by all three so the blue and orange lessons are read at a
    // speed the white one already taught
    private const float SweepSpeed = 190f;

    // ── geometry ─────────────────────────────────────────────────────────────
    // fat enough that blue/orange are unmistakably passing THROUGH you rather
    // than clipping an edge — the rule only teaches if the overlap is visible
    private const int LineWidth = 12;

    // the hole in the white line. generous on purpose, this lesson is only
    // "you can move", it isn't meant to be threatening
    private const int GapHeight = 46;

    // keeps the gap off the very top and bottom of the arena, so it's never a
    // pixel-perfect squeeze into a corner
    private const int GapInset = 10;

    private const int Damage = 3; // out of 20, so a fumbled lesson isn't fatal

    // pulses while parked, so it's clear the colour means something before it
    // starts moving
    private const float WarnPulseHz = 4f;

    private const string WarnSound  = "snd_spearrise";
    private const string SweepSound = "snd_grab";
    private const string HitSound   = "damage";

    // ── what it says ─────────────────────────────────────────────────────────
    private const string BeatWarn    = "warn";
    private const string BeatCleared = "cleared";
    private const string BeatHit     = "hit";

    private enum Phase { Expand, Warn, Sweep, Recover, Done }

    private Phase _phase = Phase.Expand;
    private float _timer;

    private Rectangle _arena;
    private float     _lineX;     // left edge of the line, in px
    private float     _startX;
    private float     _endX;
    private int       _gapTop;
    private float     _duration = 8f; // replaced in Start once the arena is known

    // so the closing line can react to whether the rule actually landed
    private int _hitsAtSweepStart;

    public float Duration => _duration;

    public bool IsComplete => _phase == Phase.Done;

    public void Start(DodgeContext context)
    {
        // the arena becomes the menu box — a wide, short lane. a vertical line
        // crossing a 200px square is over too fast to teach anything
        _arena = context.MenuBox;
        context.ResizeBoxTo(_arena, ExpandSeconds);

        // parks flush against the outside of the right border and leaves flush
        // past the left one. nothing here clips to the box, so starting it off
        // in open space would just read as a bar floating outside the arena
        _startX = _arena.Right;
        _endX   = _arena.Left - LineWidth;
        _lineX  = _startX;

        // random within the safe band so the white lesson isn't the same dodge
        // every time it repeats
        int lowest  = _arena.Top + GapInset;
        int highest = _arena.Bottom - GapInset - GapHeight;
        _gapTop = highest > lowest ? Random.Shared.Next(lowest, highest + 1) : lowest;

        float travel = _startX - _endX;
        _duration = ExpandSeconds + WarnSeconds + travel / SweepSpeed
                  + RecoverSeconds + DurationSlack;

        // it has to be on screen to be the one explaining the rule
        context.SetTeacherVisible(true);
        context.SetTeacherSpeech(Line(context, BeatWarn, DefaultWarn));
    }

    public void Update(GameTime gameTime, DodgeContext context)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _timer += dt;

        switch (_phase)
        {
            case Phase.Expand:
                if (_timer >= ExpandSeconds)
                {
                    SoundManager.Play(WarnSound);
                    Advance(Phase.Warn);
                }
                break;

            case Phase.Warn:
                // parked off the right edge, pulsing, rule up in the bubble.
                // nothing can hit you yet
                if (_timer >= WarnSeconds)
                {
                    _hitsAtSweepStart = context.PlayerHitCount;
                    SoundManager.Play(SweepSound);
                    Advance(Phase.Sweep);
                }
                break;

            case Phase.Sweep:
                _lineX -= SweepSpeed * dt;
                ApplyDamage(context);

                if (_lineX <= _endX)
                {
                    _lineX = _endX;
                    bool clean = context.PlayerHitCount == _hitsAtSweepStart;
                    context.SetTeacherSpeech(clean
                        ? Line(context, BeatCleared, DefaultCleared)
                        : Line(context, BeatHit,     DefaultHit));
                    Advance(Phase.Recover);
                }
                break;

            case Phase.Recover:
                // holds on the closing line so the rule gets stated twice — once
                // before it moved and once against what actually happened
                if (_timer >= RecoverSeconds) Advance(Phase.Done);
                break;
        }
    }

    // ── damage ───────────────────────────────────────────────────────────────

    private void ApplyDamage(DodgeContext context)
    {
        if (!_kind.Connects(context.IsPlayerMoving)) return;

        int half = GameSettings.DodgeHitboxSize / 2;
        var soul = new Rectangle(
            (int)context.HitboxPosition.X - half, (int)context.HitboxPosition.Y - half,
            GameSettings.DodgeHitboxSize, GameSettings.DodgeHitboxSize);

        foreach (Rectangle segment in Segments())
        {
            if (!segment.Intersects(soul)) continue;

            // DodgePhase's i-frames stop this repeating every frame the line is
            // still overlapping, so it costs one hit per pass rather than ten
            context.DamagePlayer(Damage);
            SoundManager.Play(HitSound);
            return;
        }
    }

    // the white line is split by its gap, the colours are one solid bar. both
    // collision and drawing read this, so they can never disagree
    private Rectangle[] Segments()
    {
        int x = (int)_lineX;

        if (_kind != HazardRule.White)
            return new[] { new Rectangle(x, _arena.Top, LineWidth, _arena.Height) };

        int gapBottom = _gapTop + GapHeight;
        return new[]
        {
            new Rectangle(x, _arena.Top, LineWidth, _gapTop - _arena.Top),
            new Rectangle(x, gapBottom,  LineWidth, _arena.Bottom - gapBottom),
        };
    }

    // ── drawing ──────────────────────────────────────────────────────────────

    public void Draw(SpriteBatch sb, Texture2D pixel)
    {
        if (_phase is Phase.Expand or Phase.Done) return;

        Color color = LineColor;

        // while it's parked the colour flashes toward white, so a colourblind
        // read still gets "this one is doing something different"
        if (_phase == Phase.Warn)
        {
            float pulse = (MathF.Sin(_timer * WarnPulseHz * MathHelper.TwoPi) + 1f) * 0.5f;
            color = Color.Lerp(color, Color.White, pulse * 0.45f);
        }

        foreach (Rectangle segment in Segments())
            if (segment.Height > 0) sb.Draw(pixel, segment, color);
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private Color LineColor => _kind.Tint();

    // one variant per kind isn't needed — each colour is its own move in the
    // JSON, so each gets its own speech block and index 0 is the only entry
    private string Line(DodgeContext context, string beat, string fallback)
        => context.Line(beat, 0, fallback);

    private string DefaultWarn => _kind switch
    {
        HazardRule.Blue   =>"BLUE. Do NOT move. Hold still and let it pass.",
        HazardRule.Orange =>"ORANGE. Keep MOVING. Walk straight through it.",
        _           => "Move to the gap. Anything white will hurt you.",
    };

    private string DefaultCleared => _kind switch
    {
        HazardRule.Blue   =>"That's blue. Still means safe.",
        HazardRule.Orange =>"That's orange. Moving means safe.",
        _           => "That's dodging. Now the colours.",
    };

    private string DefaultHit => _kind switch
    {
        HazardRule.Blue   =>"You moved. BLUE only hurts you if you move.",
        HazardRule.Orange =>"You stopped. ORANGE only hurts you if you stand still.",
        _           => "White always hurts. Get into the gap.",
    };

    private void Advance(Phase next)
    {
        _phase = next;
        _timer = 0f;
    }
}
