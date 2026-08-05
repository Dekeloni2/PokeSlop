using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FinalProject.Core;
using FinalProject.Core.Graphics;

namespace FinalProject.Battle.Patterns;

// Yakir's second lesson: conditionals. he shows a line of code in his bubble,
// the player has to work out which branch runs and stand in the zone it does
// NOT attack. warning markers flash on the doomed zone, then it rains.
// the last "question" is broken code, so it just throws a compiler error and
// nothing happens, which is the joke
public class ConditionalPattern : IBulletPattern
{
    // ends when the last question resolves, not on a timer
    public float Duration => _doneTime >= 0f ? _doneTime + EndPause : 60f;

    private const float EndPause = 1.4f;

    // ── timing ───────────────────────────────────────────────────────────────
    private const float IntroSeconds     = 3.0f;
    private const float ReadSeconds      = 2.6f;  // time to parse the code line
    private const float TelegraphSeconds = 0.55f; // markers flash on the doomed zone
    private const float RainSeconds      = 2.2f;
    private const float PauseSeconds     = 0.7f;  // beat between questions
    private const float ErrorSeconds     = 3.0f;  // the compiler error hangs there

    private const float SpawnInterval    = 0.04f;
    private const float RainSpeedScale   = 1.9f;  // of DodgeProjectileSpeed

    // a thin trickle over the whole arena while a branch is firing. standing
    // in the safe half shouldn't mean standing still, it should mean surviving
    private const float StraySpawnInterval = 0.3f;
    private const float WarningFlashSeconds = 0.08f; // per frame, like garlic gun
    private const int   WarningMarkers   = 3;

    // same staging as the data types lesson, low box with him standing over it.
    // tighter than the quiz box on purpose, a half of a 400px arena is more
    // open floor than a dodge needs
    // BoxTop is raised enough that the bottom edge (BoxTop + BoxHeight)
    // clears the HP bar's row (y=390) with some margin, same box size
    private const int BoxWidth  = 320;
    private const int BoxHeight = 170;
    private const int BoxTop    = 210;
    private const float ResizeSeconds = 0.6f;

    private enum Zone { Left, Right, Top, Bottom }
    private enum Phase { Intro, Read, Telegraph, Rain, Error, Pause, Done }

    // one line of the lesson. Eval null means the code doesn't compile, that's
    // the gag question, nothing fires and the error text shows instead
    private class Question
    {
        public string Code;
        public Func<DodgeContext, bool> Eval;
        public Zone IfTrue, IfFalse;
        public string ErrorText;
    }

    private static readonly Question[] Questions =
    {
        new Question {
            Code   = "if (10 > 5) attackLeft(); else attackRight();",
            Eval   = _ => 10 > 5,
            IfTrue = Zone.Left, IfFalse = Zone.Right },
        new Question {
            Code   = "if (2 + 2 == 5) attackTop(); else attackBottom();",
            Eval   = _ => false,
            IfTrue = Zone.Top, IfFalse = Zone.Bottom },
        new Question {
            Code   = "if (!(3 > 1)) attackLeft(); else attackRight();",
            Eval   = _ => !(3 > 1),
            IfTrue = Zone.Left, IfFalse = Zone.Right },
        new Question {
            // evaluated against where the soul actually is when the read
            // window closes, so the player has to make the condition miss
            Code   = "if (soul.x < middle) attackLeft(); else attackRight();",
            Eval   = ctx => ctx.HitboxPosition.X < ctx.CurrentBox.Center.X,
            IfTrue = Zone.Left, IfFalse = Zone.Right },
        new Question {
            Code      = "if (\"ten\" == 10) attackEverything();",
            Eval      = null,
            // single line on purpose, the mid attack bubble can't page on Z
            ErrorText = "Hah! Tricked ya! Strings can't be ints!" },
    };

    private Phase _phase = Phase.Intro;
    private float _phaseTimer;
    private float _elapsed;
    private float _spawnTimer;
    private float _strayTimer;
    private float _doneTime = -1f;
    private int   _question;
    private Zone  _attackZone;
    private readonly Random _rng = new();

    // what goes in his speech bubble, BattleState keeps it synced. the gloat
    // holds through the pause after the trick question, and once he's said it
    // he's done talking, null drops the bubble entirely
    private string PromptText => _phase switch
    {
        Phase.Intro => "* Lesson two: conditionals! The computer checks, then it acts.",
        Phase.Error => "* " + Questions[_question].ErrorText,
        Phase.Pause when Questions[_question].Eval == null
                    => "* " + Questions[_question].ErrorText,
        Phase.Done  => null,
        _           => "* " + Questions[_question].Code,
    };

    public void Start(DodgeContext context)
    {
        context.ResizeBoxTo(
            new Rectangle(GameSettings.WindowWidth / 2 - BoxWidth / 2, BoxTop,
                          BoxWidth, BoxHeight),
            ResizeSeconds);

        context.SetTeacherVisible(true);
        context.SetTeacherSpeech(PromptText);
    }

    public void Update(GameTime gameTime, DodgeContext context)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _elapsed    += dt;
        _phaseTimer += dt;

        context.SetTeacherSpeech(PromptText);

        switch (_phase)
        {
            case Phase.Intro:
                if (_phaseTimer >= IntroSeconds) Advance(Phase.Read);
                break;

            case Phase.Read:
                if (_phaseTimer < ReadSeconds) break;

                Question q = Questions[_question];
                if (q.Eval == null)
                {
                    Advance(Phase.Error); // doesn't compile, nothing to dodge
                }
                else
                {
                    // the condition is settled here, including the one that
                    // reads the soul's position
                    _attackZone = q.Eval(context) ? q.IfTrue : q.IfFalse;
                    Advance(Phase.Telegraph);
                }
                break;

            case Phase.Telegraph:
                if (_phaseTimer >= TelegraphSeconds) Advance(Phase.Rain);
                break;

            case Phase.Rain:
                SpawnRain(context, dt);
                if (_phaseTimer >= RainSeconds) Advance(Phase.Pause);
                break;

            case Phase.Error:
                if (_phaseTimer >= ErrorSeconds) Advance(Phase.Pause);
                break;

            case Phase.Pause:
                if (_phaseTimer < PauseSeconds) break;

                _question++;
                if (_question >= Questions.Length)
                {
                    _doneTime = _elapsed;
                    Advance(Phase.Done);
                }
                else
                {
                    Advance(Phase.Read);
                }
                break;
        }
    }

    private void Advance(Phase next)
    {
        _phase      = next;
        _phaseTimer = 0f;
        _spawnTimer = 0f;
    }

    // half of the arena, the part the branch that ran is attacking
    private static Rectangle ZoneRect(Rectangle box, Zone zone) => zone switch
    {
        Zone.Left   => new Rectangle(box.Left, box.Top, box.Width / 2, box.Height),
        Zone.Right  => new Rectangle(box.Center.X, box.Top, box.Width / 2, box.Height),
        Zone.Top    => new Rectangle(box.Left, box.Top, box.Width, box.Height / 2),
        _           => new Rectangle(box.Left, box.Center.Y, box.Width, box.Height / 2),
    };

    private void SpawnRain(DodgeContext context, float dt)
    {
        SpawnStray(context, dt);

        _spawnTimer += dt;
        if (_spawnTimer < SpawnInterval) return;
        _spawnTimer = 0f;

        Rectangle box  = context.CurrentBox;
        Rectangle zone = ZoneRect(box, _attackZone);
        float speed = GameSettings.DodgeProjectileSpeed * RainSpeedScale;

        // side zones rain from the top, top/bottom zones stream in from the
        // right, so the bullets always travel along the zone's long side
        if (_attackZone == Zone.Left || _attackZone == Zone.Right)
        {
            float x = zone.Left + (float)_rng.NextDouble() * zone.Width;
            context.SpawnProjectile(new Vector2(x, box.Top), new Vector2(0f, speed),
                                    ProjectileType.Code);
        }
        else
        {
            float y = zone.Top + (float)_rng.NextDouble() * zone.Height;
            context.SpawnProjectile(new Vector2(box.Right, y), new Vector2(-speed, 0f),
                                    ProjectileType.Code);
        }
    }

    // the odd bullet anywhere in the arena, slower than the branch rain so it
    // reads as background noise rather than a second attack to solve
    private void SpawnStray(DodgeContext context, float dt)
    {
        _strayTimer += dt;
        if (_strayTimer < StraySpawnInterval) return;
        _strayTimer = 0f;

        Rectangle box = context.CurrentBox;
        float x = box.Left + (float)_rng.NextDouble() * box.Width;

        context.SpawnProjectile(new Vector2(x, box.Top),
            new Vector2(0f, GameSettings.DodgeProjectileSpeed), ProjectileType.Code);
    }

    // flashing markers over the doomed zone while it telegraphs. drawn by
    // DodgePhase, same hand off as the quiz buttons
    public void DrawUi(SpriteBatch spriteBatch, Texture2D pixel, SpriteFont font, Rectangle box)
    {
        if (_phase != Phase.Telegraph) return;

        Spritesheet warning = SpriteManager.GetSprite("warning");
        if (warning == null) return;

        int frame = (int)(_phaseTimer / WarningFlashSeconds) % warning.Columns;
        Rectangle src  = warning[frame, 0];
        Rectangle zone = ZoneRect(box, _attackZone);

        bool sideways = _attackZone == Zone.Top || _attackZone == Zone.Bottom;

        for (int i = 0; i < WarningMarkers; i++)
        {
            // spread along the zone's long axis, centred on the short one
            float t = (i + 1) / (float)(WarningMarkers + 1);

            int cx = sideways ? zone.Left + (int)(zone.Width * t) : zone.Center.X;
            int cy = sideways ? zone.Center.Y : zone.Top + (int)(zone.Height * t);

            spriteBatch.Draw(warning.Texture,
                new Rectangle(cx - src.Width / 2, cy - src.Height / 2, src.Width, src.Height),
                src, Color.White);
        }
    }
}
