using System;
using Microsoft.Xna.Framework;
using FinalProject.Core;

namespace FinalProject.Battle.Patterns;

// Yakir's fourth lesson: everything at once. he writes an actual program line
// by line and each line runs against the battle box itself, variables set its
// size, for loops shrink it in steps, ifs read where the soul is standing and
// push that wall in, the box gets dragged around the screen, the rain rate
// gets cranked, and a Thread.Sleep holds you in the tiny box at the worst of
// it. box.Reset() puts everything back
public class ProgramPattern : IBulletPattern
{
    public float Duration => _doneTime >= 0f ? _doneTime + EndPause : 120f;

    private const float EndPause = 1.2f;

    // ── timing ───────────────────────────────────────────────────────────────
    private const float IntroSeconds = 3.0f;
    private const float OutroSeconds = 2.8f;   // he takes a bow after it compiles
    private const float ShowSeconds  = 1.7f;   // reading each line before it runs
    private const float BaseRainInterval  = 0.24f; // gentle, the box is the attack
    private const float HeavyRainInterval = 0.09f; // after spawner.interval /= 3
    private const float RainSpeedScale = 1.3f;
    private const float LoopStepSeconds = 0.5f; // gap between for-loop chunks

    // same staging as the rest of the course
    private const int BoxWidth  = 400;
    private const int BoxHeight = 200;
    private const int BoxTop    = 252;
    private const float ResizeSeconds = 0.6f;

    private enum Fx { Grab, SetWidth, LoopWidth, PushX, Slide, LoopHeight, PushY, RainUp, Grind, Sleep, Reset }
    private enum Phase { Intro, Show, Execute, Outro, Done }

    // one line of his program: what he says, what it does, how long it runs,
    // and a spare number for the lines that need one (slide distance)
    private class Step
    {
        public string Code;
        public Fx     Fx;
        public float  Seconds;
        public int    Arg;
    }

    private static readonly Step[] Steps =
    {
        new Step { Code = "var box = GetBattleBox();",                                Fx = Fx.Grab,       Seconds = 0.8f },
        new Step { Code = "box.width = 300;",                                         Fx = Fx.SetWidth,   Seconds = 0.8f },
        new Step { Code = "for (int i = 0; i < 4; i++) box.width -= 20;",             Fx = Fx.LoopWidth,  Seconds = 2.4f },
        new Step { Code = "if (soul.x < middle) box.left += 50; else box.right -= 50;", Fx = Fx.PushX,    Seconds = 1.0f },
        new Step { Code = "box.x += 60;",                                             Fx = Fx.Slide,      Seconds = 1.2f, Arg =  60 },
        new Step { Code = "box.x -= 120;",                                            Fx = Fx.Slide,      Seconds = 1.2f, Arg = -120 },
        new Step { Code = "box.x += 60;",                                             Fx = Fx.Slide,      Seconds = 1.0f, Arg =  60 },
        new Step { Code = "for (int i = 0; i < 3; i++) box.height -= 25;",            Fx = Fx.LoopHeight, Seconds = 1.8f },
        new Step { Code = "if (soul.y < middle) box.top += 35; else box.bottom -= 35;", Fx = Fx.PushY,    Seconds = 1.0f },
        new Step { Code = "spawner.interval /= 3;",                                   Fx = Fx.RainUp,     Seconds = 0.8f },
        new Step { Code = "while (box.width > 130) box.width -= 5;",                  Fx = Fx.Grind,      Seconds = 3.0f },
        new Step { Code = "Thread.Sleep(4000);",                                      Fx = Fx.Sleep,      Seconds = 4.0f },
        new Step { Code = "box.Reset();",                                             Fx = Fx.Reset,      Seconds = 1.0f },
    };

    private Phase _phase = Phase.Intro;
    private float _phaseTimer;
    private float _elapsed;
    private float _spawnTimer;
    private float _doneTime = -1f;

    private int   _step;
    private int   _loopSub;      // which iteration of a for line has applied
    private bool  _raining;
    private float _rainInterval = BaseRainInterval;
    private Rectangle _box;      // the program's view of the arena
    private readonly Random _rng = new();

    private string PromptText => _phase switch
    {
        Phase.Intro => "* Final lesson. We put it all together and write a real program.",
        Phase.Outro => "* See? Isn't coding so much fun!",
        Phase.Done  => null, // he's taken his bow
        Phase.Execute when Steps[_step].Fx is Fx.LoopWidth or Fx.LoopHeight
                    => "* i = " + Math.Min(_loopSub, LoopCount(Steps[_step].Fx) - 1),
        Phase.Execute when Steps[_step].Fx == Fx.Sleep
                    => "* ...zzz.",
        _           => "* " + Steps[_step].Code,
    };

    private static int LoopCount(Fx fx) => fx == Fx.LoopWidth ? 4 : 3;

    public void Start(DodgeContext context)
    {
        _box = new Rectangle(GameSettings.WindowWidth / 2 - BoxWidth / 2, BoxTop,
                             BoxWidth, BoxHeight);

        context.ResizeBoxTo(_box, ResizeSeconds);
        context.SetTeacherVisible(true);
        context.SetTeacherSpeech(PromptText);
    }

    public void Update(GameTime gameTime, DodgeContext context)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _elapsed    += dt;
        _phaseTimer += dt;

        context.SetTeacherSpeech(PromptText);

        if (_raining) SpawnRain(context, dt);

        switch (_phase)
        {
            case Phase.Intro:
                if (_phaseTimer >= IntroSeconds) Advance(Phase.Show);
                break;

            case Phase.Show:
                if (_phaseTimer >= ShowSeconds)
                {
                    BeginEffect(context);
                    Advance(Phase.Execute);
                }
                break;

            case Phase.Execute:
                UpdateEffect(context);

                if (_phaseTimer >= Steps[_step].Seconds)
                {
                    _step++;
                    if (_step >= Steps.Length) Advance(Phase.Outro);
                    else                       Advance(Phase.Show);
                }
                break;

            case Phase.Outro:
                if (_phaseTimer >= OutroSeconds)
                {
                    _doneTime = _elapsed;
                    Advance(Phase.Done);
                }
                break;
        }
    }

    // the line has been read, now it runs
    private void BeginEffect(DodgeContext context)
    {
        _loopSub = 0;

        switch (Steps[_step].Fx)
        {
            case Fx.Grab:
                // the program takes hold of the arena, a little jolt sells it
                context.ShakeScreen(4f, 0.3f);
                break;

            case Fx.SetWidth:
                _box = new Rectangle(_box.Center.X - 150, _box.Y, 300, _box.Height);
                context.ResizeBoxTo(_box, 0.5f);
                _raining = true; // the program is running now, rain until Reset
                break;

            case Fx.PushX:
                // reads where the soul actually is, the wall on that side comes in
                if (context.HitboxPosition.X < _box.Center.X)
                    _box = new Rectangle(_box.X + 50, _box.Y, _box.Width - 50, _box.Height);
                else
                    _box = new Rectangle(_box.X, _box.Y, _box.Width - 50, _box.Height);
                context.ResizeBoxTo(_box, 0.5f);
                break;

            case Fx.PushY:
                // same trick vertically, ceiling or floor depending on the soul
                if (context.HitboxPosition.Y < _box.Center.Y)
                    _box = new Rectangle(_box.X, _box.Y + 35, _box.Width, _box.Height - 35);
                else
                    _box = new Rectangle(_box.X, _box.Y, _box.Width, _box.Height - 35);
                context.ResizeBoxTo(_box, 0.5f);
                break;

            case Fx.Slide:
                // the whole box moves and the clamp drags the soul along with it
                _box = new Rectangle(_box.X + Steps[_step].Arg, _box.Y, _box.Width, _box.Height);
                context.ResizeBoxTo(_box, Steps[_step].Seconds * 0.8f);
                break;

            case Fx.RainUp:
                _rainInterval = HeavyRainInterval;
                break;

            case Fx.Grind:
                // one long tween reads as the while loop chewing it down
                _box = new Rectangle(_box.Center.X - 65, _box.Y, 130, _box.Height);
                context.ResizeBoxTo(_box, 2.6f);
                break;

            case Fx.Sleep:
                // nothing happens. that's the punchline, you're stuck in the
                // tiny box under the heavy rain while the program naps
                break;

            case Fx.Reset:
                _raining      = false;
                _rainInterval = BaseRainInterval;
                _box = new Rectangle(GameSettings.WindowWidth / 2 - BoxWidth / 2, BoxTop,
                                     BoxWidth, BoxHeight);
                context.ResizeBoxTo(_box, 0.8f);
                break;
        }
    }

    // the for lines shrink in visible chunks while i ticks in the bubble
    private void UpdateEffect(DodgeContext context)
    {
        Fx fx = Steps[_step].Fx;
        if (fx != Fx.LoopWidth && fx != Fx.LoopHeight) return;

        if (_loopSub < LoopCount(fx) && _phaseTimer >= _loopSub * LoopStepSeconds)
        {
            _box = fx == Fx.LoopWidth
                ? new Rectangle(_box.X + 10, _box.Y, _box.Width - 20, _box.Height)
                : new Rectangle(_box.X, _box.Y + 12, _box.Width, _box.Height - 25);

            context.ResizeBoxTo(_box, 0.3f);
            _loopSub++;
        }
    }

    private void Advance(Phase next)
    {
        _phase      = next;
        _phaseTimer = 0f;
    }

    private void SpawnRain(DodgeContext context, float dt)
    {
        _spawnTimer += dt;
        if (_spawnTimer < _rainInterval) return;
        _spawnTimer = 0f;

        Rectangle box = context.CurrentBox;
        float x = box.Left + (float)_rng.NextDouble() * box.Width;

        context.SpawnProjectile(
            new Vector2(x, box.Top),
            new Vector2(0f, GameSettings.DodgeProjectileSpeed * RainSpeedScale),
            ProjectileType.Code);
    }
}
