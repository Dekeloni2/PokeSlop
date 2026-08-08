using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FinalProject.Core;
using FinalProject.Core.Graphics;

using FinalProject.Battle.Dodge;
using FinalProject.Battle.Hazards;
namespace FinalProject.Battle.Patterns;

// Yakir's third lesson: loops. the arena splits into columns and his bubble
// shows the counter ticking, each iteration rains on column i, so reading the
// loop is how you stay ahead of it. first a plain 0..4 sweep, then one with a
// step of 2, then a while(true) that has no exit condition, climbs into a
// panic and only stops when he ctrl+c's the whole thing
public class LoopPattern : IBulletPattern
{
    // ends when he kills the runaway loop, not on a timer
    public float Duration => _doneTime >= 0f ? _doneTime + EndPause : 90f;

    private const float EndPause = 1.2f;

    // ── timing ───────────────────────────────────────────────────────────────
    private const float IntroSeconds     = 3.0f;
    private const float AnnounceSeconds  = 2.2f;  // reading the loop header
    private const float TelegraphSeconds = 0.38f; // marker on the column
    private const float RainSeconds      = 0.65f; // the burst itself
    private const float CtrlCSeconds     = 2.0f;  // he holds the panic line
    private const float SpawnInterval    = 0.035f;
    private const float RainSpeedScale   = 2.0f;  // of DodgeProjectileSpeed

    // once the runaway loop is properly out of control it stops politely doing
    // one column at a time and drags the previous one along with it
    private const int TrailingFrom = 3;

    // the runaway loop gets faster every iteration, down to a floor
    private const float InfiniteAccel    = 0.85f; // per iteration multiplier
    private const float InfiniteMinScale = 0.4f;
    private const int   CtrlCAt          = 9;     // iterations before he pulls the plug

    private const int Columns = 5;

    // same staging as the other lessons, tightened so a column is somewhere you
    // have to leave rather than somewhere you stroll past
    // BoxTop is raised enough that the bottom edge (BoxTop + BoxHeight)
    // clears the HP bar's row (y=390) with some margin, same box size
    private const int BoxWidth  = 320;
    private const int BoxHeight = 170;
    private const int BoxTop    = 210;
    private const float ResizeSeconds = 0.6f;
    private const float WarningFlashSeconds = 0.08f;

    private enum Phase { Intro, Announce, Telegraph, Rain, CtrlC, Done }

    // a finite loop is its header plus which i values it visits. Steps null
    // means while(true), i just keeps climbing until the ctrl+c
    private class LoopDef
    {
        public string Code;
        public int[]  Steps;
    }

    private static readonly LoopDef[] Loops =
    {
        new LoopDef { Code = "for (int i = 0; i < 5; i++)",    Steps = new[] { 0, 1, 2, 3, 4 } },
        new LoopDef { Code = "for (int i = 0; i < 5; i += 2)", Steps = new[] { 0, 2, 4 } },
        new LoopDef { Code = "while (true) attackColumn(i);",  Steps = null },
    };

    private Phase _phase = Phase.Intro;
    private float _phaseTimer;
    private float _elapsed;
    private float _spawnTimer;
    private float _doneTime = -1f;

    private int _loop;
    private int _step; // index into Steps, or the raw i for while(true)
    private readonly Random _rng = new();

    private bool IsInfinite  => Loops[_loop].Steps == null;
    private int  CurrentI    => IsInfinite ? _step : Loops[_loop].Steps[_step];
    private int  CurrentColumn => CurrentI % Columns;

    // the runaway loop speeds up as i climbs, the taught ones run at full time
    private float SpeedScale => IsInfinite
        ? MathF.Max(InfiniteMinScale, MathF.Pow(InfiniteAccel, _step))
        : 1f;

    private string PromptText => _phase switch
    {
        Phase.Intro    => "* Lesson three: loops! Why do something once when you can do it five times?",
        Phase.Announce => "* " + Loops[_loop].Code,
        Phase.CtrlC    => "* CTRL+C! CTRL+C!",
        Phase.Done     => null, // he's said enough
        _ when IsInfinite && _step >= 8 => "* WHERE IS MY EXIT CONDITION",
        _ when IsInfinite && _step == 7 => "* Where is my exit condition?",
        _ when IsInfinite && _step == 6 => "* i = 6... wait.",
        _              => "* i = " + CurrentI,
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
                if (_phaseTimer >= IntroSeconds) Advance(Phase.Announce);
                break;

            case Phase.Announce:
                if (_phaseTimer >= AnnounceSeconds)
                {
                    _step = 0;
                    Advance(Phase.Telegraph);
                }
                break;

            case Phase.Telegraph:
                if (_phaseTimer >= TelegraphSeconds * SpeedScale) Advance(Phase.Rain);
                break;

            case Phase.Rain:
                SpawnRain(context, dt);
                if (_phaseTimer >= RainSeconds * SpeedScale) NextIteration(context);
                break;

            case Phase.CtrlC:
                if (_phaseTimer >= CtrlCSeconds)
                {
                    _doneTime = _elapsed;
                    Advance(Phase.Done);
                }
                break;
        }
    }

    private void NextIteration(DodgeContext context)
    {
        if (IsInfinite)
        {
            _step++;

            // no exit condition, so HE is the exit condition. the wipe is the
            // whole punchline, everything stops the instant he kills it
            if (_step >= CtrlCAt)
            {
                context.ClearProjectiles();
                Advance(Phase.CtrlC);
                return;
            }

            Advance(Phase.Telegraph);
            return;
        }

        _step++;
        if (_step < Loops[_loop].Steps.Length)
        {
            Advance(Phase.Telegraph);
            return;
        }

        // this loop is done, on to the next one
        _loop++;
        Advance(Phase.Announce);
    }

    private void Advance(Phase next)
    {
        _phase      = next;
        _phaseTimer = 0f;
        _spawnTimer = 0f;
    }

    private static Rectangle ColumnRect(Rectangle box, int column)
    {
        int w = box.Width / Columns;
        return new Rectangle(box.Left + column * w, box.Top, w, box.Height);
    }

    private void SpawnRain(DodgeContext context, float dt)
    {
        _spawnTimer += dt;
        if (_spawnTimer < SpawnInterval) return;
        _spawnTimer = 0f;

        RainOnColumn(context, CurrentColumn);

        // the runaway loop smears across two columns once it's gone properly
        // wrong, so there's no standing still until he pulls the plug
        if (IsInfinite && _step >= TrailingFrom)
            RainOnColumn(context, (CurrentColumn + Columns - 1) % Columns);
    }

    private void RainOnColumn(DodgeContext context, int column)
    {
        Rectangle col = ColumnRect(context.CurrentBox, column);
        float x = col.Left + (float)_rng.NextDouble() * col.Width;

        context.SpawnProjectile(
            new Vector2(x, context.CurrentBox.Top),
            new Vector2(0f, GameSettings.DodgeProjectileSpeed * RainSpeedScale),
            ProjectileType.Code);
    }

    // flashing marker on the column that's about to get it
    public void DrawUi(SpriteBatch spriteBatch, Texture2D pixel, SpriteFont font, Rectangle box)
    {
        if (_phase != Phase.Telegraph) return;

        Spritesheet warning = SpriteManager.GetSprite("warning");
        if (warning == null) return;

        int frame = (int)(_phaseTimer / WarningFlashSeconds) % warning.Columns;
        Rectangle src = warning[frame, 0];
        Rectangle col = ColumnRect(box, CurrentColumn);

        spriteBatch.Draw(warning.Texture,
            new Rectangle(col.Center.X - src.Width / 2, col.Center.Y - src.Height / 2,
                          src.Width, src.Height),
            src, Color.White);
    }
}
