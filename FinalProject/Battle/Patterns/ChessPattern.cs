using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FinalProject.Core.Audio;
using FinalProject.Core.Graphics;

namespace FinalProject.Battle.Patterns;

// Dor's chess board. "Simple to learn, hard to master" is his line about chess,
// so this is the attack that takes your movement away and makes you think a
// move ahead instead of reacting.
//
// The arena grows to a 280x280 board — a 4px frame around 8x8 tiles of 34px,
// which is exactly what the 32px pieces want. The soul stops moving freely and
// steps one tile at a time (PlayerHitbox's grid mode). Pieces drop onto the top
// row and hunt it using real chess moves: the rook slides any distance in a
// straight line, the knight jumps its L, the pawn shuffles one square, and the
// queen does everything the rook does plus diagonals.
//
// It gets heavier every time he sets the board — three pieces the first time,
// one more each attack until the back rank is full at eight. The first queen
// shows up on the third board and the second on the fifth, and two is the cap.
//
// Only one piece moves at a time. It flashes red along the squares it's about
// to cross, then goes. Land on the soul and it hits you and keeps hunting.
//
// Stepping onto a piece takes a point of health off it, two to knock it off the
// board — but the piece that's mid-telegraph is untouchable, so you can't just
// walk through the board. The red flash means both "this is where it's going"
// and "don't try it". A piece that survives your hit throws you back to the
// square you came from; one you finish off leaves you standing on its square.
// Clear the board and the attack ends early, otherwise you just have to survive.
public class ChessPattern : IBulletPattern
{
    // ── board geometry ───────────────────────────────────────────────────────
    // 4 + (8 x 34) + 4 == 280 exactly, so the frame and the tiles both land on
    // whole pixels and the pieces sit in their squares with a 1px margin
    private const int BoxSize  = 280;
    private const int FramePx  = 4;
    private const int TilePx   = 34;
    private const int Cols     = 8;
    private const int Rows     = 8;

    private const int PieceSrc = 32; // chess.png is 128x32, four 32x32 frames

    // ── escalation ───────────────────────────────────────────────────────────
    // he sets a heavier board every time. one more piece per attack, capped at
    // the width of the back rank since they all come down on the top row, and
    // queens start turning up once you've seen it a few times
    private const int BasePieces     = 3;
    private const int MaxPieces      = Cols; // 8, one per column of the top row
    private const int FirstQueenUse  = 3;
    private const int SecondQueenUse = 5;
    private const int MaxQueens      = 2;

    private static int _timesUsed;

    // ── what he says ─────────────────────────────────────────────────────────
    // four beats per board, so he isn't repeating the same two lines across six
    // increasingly nasty versions of the same attack, and so taking his board
    // apart gets a different answer from running the clock down.
    //
    // the real lines live in the teacher's JSON under the move's "speech" block,
    // keyed by these beat names, indexed by board number. this table is only the
    // fallback for when a teacher doesn't author a beat — it's what you see if
    // the JSON is missing, so it's worth keeping readable rather than blank.
    // keep lines under ~50 characters, that's about what the bubble fits
    private const string BeatOpening  = "opening";
    private const string BeatLanding  = "landing";
    private const string BeatCleared  = "cleared";
    private const string BeatSurvived = "survived";
    private readonly struct Script
    {
        public readonly string Opening;  // as the board expands
        public readonly string Landing;  // as the pieces hit it
        public readonly string Cleared;  // you took every piece
        public readonly string Survived; // the clock ran out with pieces left

        public Script(string opening, string landing, string cleared, string survived)
        {
            Opening  = opening;
            Landing  = landing;
            Cleared  = cleared;
            Survived = survived;
        }
    }

    private static readonly Script[] Scripts =
    {
        // 3 pieces — the pitch
        new("Chess. Simple to learn.",
            "Hard to master.",
            "Luck. Nothing more.",
            "Survival is not skill. Anyone can wait."),

        // 4 pieces — he starts scaling it
        new("Again. This time I add a piece.",
            "Everything scales. Keep up.",
            "Faster than last time. Barely.",
            "Hiding is not a strategy. It is a delay."),

        // 5 pieces, first queen
        new("You have earned a queen. Congratulations.",
            "The queen does not forgive.",
            "You took my queen. Do not enjoy it.",
            "A whole turn of nothing. Impressive."),

        // 6 pieces — the commitment speech, his favourite subject
        new("Six pieces. Still think this is a game?",
            "Commitment. That is what this tests.",
            "...You are not what I expected.",
            "You survive. You do not win. Learn that."),

        // 7 pieces, second queen
        new("Two queens. I am done being generous.",
            "The ocean shows no mercy. Neither do I.",
            "Both of them? ...Impossible.",
            "Still breathing. That will change."),

        // 8 pieces, the full back rank, and every board after
        new("The full rank. No more lessons.",
            "Soon all the world shall know this board.",
            "You cleared it. I will not forget this.",
            "You endure. The storm does not tire."),
    };

    private static Script CurrentScript
        => Scripts[Math.Clamp(_timesUsed - 1, 0, Scripts.Length - 1)];

    // the board number is the variant index, so the JSON arrays line up one
    // entry per board the same way the fallback table does
    private static string Line(DodgeContext context, string beat, string fallback)
        => context.Line(beat, _timesUsed - 1, fallback);

    // ── timing ───────────────────────────────────────────────────────────────
    private const float ExpandSeconds   = 0.6f;
    private const float TileStagger     = 0.012f; // gap between one tile and the next
    private const float TileFadeSeconds = 0.15f;  // how long a single tile takes
    private const float SummonSeconds   = 0.55f;  // pieces falling in
    private const float LandSeconds     = 0.35f;  // squash and back
    private const float PlaySeconds     = 12f;    // survive this long if you can't clear it
    // he gets the last word in here and the bubble types it a character at a
    // time, so this is paced to let a closing line land rather than flash past
    private const float RecoverSeconds  = 1.9f;

    // one piece acts roughly every half second — no turns, just relentless
    private const float TelegraphSeconds = 0.32f;
    private const float MoveSeconds      = 0.13f;
    private const float GapSeconds       = 0.10f;
    private const float TelegraphPulseHz = 7f;

    private const float SquashAmount = 0.7f;
    private const int   PieceDamage  = 5;

    // two hits to take a piece off, with a brief window after the first where it
    // can't be hit again — otherwise you'd just step off and back on and the
    // second point of health would cost nothing
    private const int   PieceHealth = 2;
    private const float HurtSeconds = 0.35f;

    // he sometimes puts a green piece on the board. taking one out pays HP back,
    // which is the only way the board ever gives anything up — worth going out
    // of your way for, and worth him hiding one on a queen
    private const float HealPieceChance  = 0.22f;
    private const float HealPercentOfMax = 0.2f;

    private static readonly Color HealGreen = new(0, 255, 0);

    // 1px in every direction, which is exactly the margin a 32px piece has in a
    // 34px square — any wider and the outline bleeds into the next tile
    private static readonly Point[] OutlineOffsets =
    {
        new(-1, 0), new(1, 0), new(0, -1), new(0, 1),
        new(-1, -1), new(1, -1), new(-1, 1), new(1, 1),
    };

    // the hit is detected a frame into the step onto the square, so ~0.07s of
    // this is the rest of that slide — the leftover is how long you're actually
    // seen standing there. keep it just above the slide time or you never
    // visibly arrive, and well below it or you're a sitting duck
    private const float KnockbackDelay = 0.15f;
    private const float KnockbackSlide = 0.06f; // recoil, snappier than a step

    // the intro is a one-off. once you've watched the board build itself there's
    // no reason to sit through it again, so later uses snap straight to a full
    // board. BattleState clears this on OnEnter so a new fight shows it again
    private static bool _boardIntroShown;

    // both statics belong to the fight, not the run — BattleState clears them
    // on OnEnter so a new battle starts from three pieces and no queens again
    public static void ResetBoard()
    {
        _boardIntroShown = false;
        _timesUsed       = 0;
    }

    private static readonly Color TelegraphRed = new(230, 60, 60);

    private const string ThudSound  = "thud";
    private const string ShakeSound = "snd_screenshake";
    private const string MoveSound  = "snd_grab";
    private const string HitSound   = "slash";          // survived it
    private const string HealSound  = SoundManager.HealSound;
    // the knockout is both cues together — vulkinhurt on top of the old
    // vaporized, pitched by the same amount so they land as one sound
    private const string TakeSound  = "snd_vulkinhurt";
    private const string TakeLayer  = "vaporized";

    // every piece goes out on a slightly different note, so clearing a crowded
    // board doesn't turn into the same sample eight times
    private const float TakePitchRange = 0.4f;

    private enum PieceKind { Knight = 0, Rook = 1, Pawn = 2, Queen = 3 }

    private class Piece
    {
        public PieceKind Kind;
        public Point     Tile;
        public float     Squash = 1f;
        public float     FallY;  // px above its resting spot while dropping in
        public bool      Alive = true;
        public int       Health = PieceHealth;
        public float     HurtLeft; // flashing, and untouchable while it runs
        public bool      Heals;    // green outlined, pays out when taken

        // knocked off the board — flies out spinning, trailing smoke
        public bool    Dying;
        public Vector2 DeathPos;
        public Vector2 DeathVel;
        public float   DeathSpin;
        public float   DeathTimer;
    }

    private enum Phase { Expand, FadeIn, Summon, Land, Play, Recover, Done }
    private enum Turn  { Telegraph, Move, Gap }

    private Phase _phase = Phase.Expand;
    private float _timer;

    private Rectangle _board;   // the whole 280 box including the frame
    private Point     _origin;  // top left of the playable tiles
    private bool      _skipIntro;

    private readonly List<Piece> _pieces = new();

    // whose move it is and where it's going
    private Turn  _turn = Turn.Telegraph;
    private float _turnTimer;
    private int   _activeIndex = -1;
    private int   _lastMoved   = -1;
    private Point _moveFrom;
    private Point _moveTo;
    private readonly List<Point> _movePath = new();

    // captures are decided by who stepped onto whom, so the soul's tile is
    // tracked frame to frame — a piece landing on a stationary soul is a hit,
    // the soul walking onto a stationary piece is a take
    private Point _lastSoulTile = new(-1, -1);

    // a hit lands, then the soul is thrown back a beat later
    private float _knockbackLeft;
    private Point _knockbackTo;

    private static readonly Point[] Orthogonal =
    {
        new(1, 0), new(-1, 0), new(0, 1), new(0, -1),
    };

    private static readonly Point[] KnightSteps =
    {
        new(1, 2), new(2, 1), new(-1, 2), new(-2, 1),
        new(1, -2), new(2, -1), new(-1, -2), new(-2, -1),
    };

    private static readonly Point[] AllEight =
    {
        new(1, 0), new(-1, 0), new(0, 1), new(0, -1),
        new(1, 1), new(1, -1), new(-1, 1), new(-1, -1),
    };

    // the three basics, cycled to fill whatever slots the queens don't take
    private static readonly PieceKind[] Basics =
    {
        PieceKind.Knight, PieceKind.Rook, PieceKind.Pawn,
    };

    private static readonly Point StartTile = new(3, 5);

    public float Duration => ExpandSeconds + FadeSeconds + SummonSeconds
                           + LandSeconds + PlaySeconds + RecoverSeconds + 0.4f;

    // the whole cascade: last tile starts at (n-1) x stagger, then fades itself
    private float FadeSeconds => _skipIntro
        ? 0f
        : (Cols * Rows - 1) * TileStagger + TileFadeSeconds;

    // clearing the board cuts the turn short instead of leaving you on an empty
    // one for the rest of the timer
    public bool IsComplete => _phase == Phase.Done;

    public void Start(DodgeContext context)
    {
        Rectangle b = context.BaseBox;
        _board  = new Rectangle(b.Center.X - BoxSize / 2, b.Center.Y - BoxSize / 2, BoxSize, BoxSize);
        _origin = new Point(_board.X + FramePx, _board.Y + FramePx);

        _skipIntro       = _boardIntroShown;
        _boardIntroShown = true;

        // the board draws its own 4px frame, the stock 2px one would sit inside
        // it. the box is tall enough to cover the HP bar, so that goes too
        context.SetBoxBorderHidden(true);
        context.SetHudHidden(true);
        context.ResizeBoxTo(_board, ExpandSeconds);

        // he runs the board from above it — the anchor follows the box, so he
        // ends up sitting just over the frame
        context.SetTeacherVisible(true);

        // counted before the script is read, so the first board gets the first row
        _timesUsed++;
        context.SetTeacherSpeech(Line(context, BeatOpening, CurrentScript.Opening));

        BuildRoster();
    }

    // one more piece every time he sets the board, up to one per column, with
    // queens arriving on their own schedule on top of that
    private void BuildRoster()
    {
        int count  = Math.Min(BasePieces + _timesUsed - 1, MaxPieces);
        int queens = _timesUsed >= SecondQueenUse ? MaxQueens
                   : _timesUsed >= FirstQueenUse  ? 1
                   :                                0;
        queens = Math.Min(queens, count);

        var kinds = new List<PieceKind>();
        for (int i = 0; i < queens; i++) kinds.Add(PieceKind.Queen);
        while (kinds.Count < count) kinds.Add(Basics[(kinds.Count - queens) % Basics.Length]);

        // shuffled so the queens aren't always parked on the same side
        for (int i = kinds.Count - 1; i > 0; i--)
        {
            int j = Random.Shared.Next(i + 1);
            (kinds[i], kinds[j]) = (kinds[j], kinds[i]);
        }

        for (int i = 0; i < count; i++)
        {
            _pieces.Add(new Piece
            {
                Kind  = kinds[i],
                Tile  = new Point(SpawnColumn(i, count), 0),
                Heals = Random.Shared.NextDouble() < HealPieceChance,
                // parked off the top from the start, otherwise they'd sit on the
                // board through the fade in and then jump up to fall again
                FallY = _board.Height + PieceSrc,
            });
        }
    }

    // spread evenly across the back rank — three sit well apart, eight fill it.
    // every count from 3 to 8 comes out with distinct columns
    private static int SpawnColumn(int index, int count)
        => count <= 1
            ? Cols / 2
            : (int)MathF.Round(index * (Cols - 1) / (float)(count - 1));

    public void Update(GameTime gameTime, DodgeContext context)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _timer += dt;

        UpdateDying(dt, context);

        switch (_phase)
        {
            case Phase.Expand:
                if (_timer >= ExpandSeconds)
                {
                    // the soul only goes on the board once the board is the size
                    // it's going to stay, otherwise it snaps to tiles that are
                    // still moving under it
                    context.EnterGrid(_origin, TilePx, Cols, Rows, StartTile);
                    _lastSoulTile = StartTile;
                    Advance(_skipIntro ? Phase.Summon : Phase.FadeIn);
                }
                break;

            case Phase.FadeIn:
                // tiles light up one at a time, left to right and down the board.
                // TileAlpha works it out per tile from _timer, no state needed
                if (_timer >= FadeSeconds) Advance(Phase.Summon);
                break;

            case Phase.Summon:
                UpdateFall();
                if (_timer >= SummonSeconds)
                {
                    foreach (Piece p in _pieces) p.FallY = 0f;
                    SoundManager.Play(ThudSound);
                    SoundManager.Play(ShakeSound);
                    context.ShakeScreen(6f, 0.25f);
                    SpawnLandingDust(context);
                    context.SetTeacherSpeech(Line(context, BeatLanding, CurrentScript.Landing));
                    Advance(Phase.Land);
                }
                break;

            case Phase.Land:
                UpdateSquash();
                if (_timer >= LandSeconds)
                {
                    foreach (Piece p in _pieces) p.Squash = 1f;
                    Advance(Phase.Play);
                }
                break;

            case Phase.Play:
                UpdatePlay(dt, context);

                // he reacts to how it actually ended — taking his board apart
                // and running out the clock deserve different answers
                if (NoneAlive)
                {
                    context.SetTeacherSpeech(Line(context, BeatCleared, CurrentScript.Cleared));
                    Advance(Phase.Recover);
                }
                else if (_timer >= PlaySeconds)
                {
                    context.SetTeacherSpeech(Line(context, BeatSurvived, CurrentScript.Survived));
                    Advance(Phase.Recover);
                }
                break;

            case Phase.Recover:
                if (_timer >= RecoverSeconds)
                {
                    context.ExitGrid();
                    Advance(Phase.Done);
                }
                break;
        }
    }

    // ── the hunt ─────────────────────────────────────────────────────────────

    private void UpdatePlay(float dt, DodgeContext context)
    {
        foreach (Piece p in _pieces)
            if (p.HurtLeft > 0f) p.HurtLeft -= dt;

        // runs first so the frame it fires, the soul's tile and _lastSoulTile
        // already agree and the bounce isn't read as a fresh step
        if (_knockbackLeft > 0f)
        {
            _knockbackLeft -= dt;
            if (_knockbackLeft <= 0f)
            {
                context.MoveSoulToTile(_knockbackTo, KnockbackSlide);
                _lastSoulTile = _knockbackTo;
            }
        }

        CheckPlayerCapture(context);

        // whoever was moving may have just been taken off the board
        if (_activeIndex >= 0 && !_pieces[_activeIndex].Alive) _activeIndex = -1;

        if (_activeIndex < 0)
        {
            if (!PickMove(context)) return; // nothing left that can move
            _turn      = Turn.Telegraph;
            _turnTimer = 0f;
        }

        _turnTimer += dt;

        switch (_turn)
        {
            case Turn.Telegraph:
                if (_turnTimer >= TelegraphSeconds)
                {
                    SoundManager.Play(MoveSound);
                    _turn = Turn.Move;
                    _turnTimer = 0f;
                }
                break;

            case Turn.Move:
                if (_turnTimer >= MoveSeconds)
                {
                    CommitMove(context);
                    _turn = Turn.Gap;
                    _turnTimer = 0f;
                }
                break;

            case Turn.Gap:
                if (_turnTimer >= GapSeconds)
                {
                    _activeIndex = -1;
                    _turnTimer   = 0f;
                }
                break;
        }
    }

    // round robins through the pieces so all three stay involved rather than one
    // doing all the chasing, then takes whichever legal move lands closest to
    // the soul. landing exactly on it scores zero, so a kill is always preferred
    private bool PickMove(DodgeContext context)
    {
        int n = _pieces.Count;

        for (int step = 1; step <= n; step++)
        {
            int i = (_lastMoved + step) % n;
            if (!_pieces[i].Alive) continue;

            List<Point> options = LegalMoves(_pieces[i]);
            if (options.Count == 0) continue;

            _activeIndex = i;
            _lastMoved   = i;
            _moveFrom    = _pieces[i].Tile;
            _moveTo      = BestMove(options, context.SoulTile);

            _movePath.Clear();
            BuildPath(_moveFrom, _moveTo, _pieces[i].Kind, _movePath);
            return true;
        }
        return false;
    }

    private List<Point> LegalMoves(Piece piece)
    {
        var moves = new List<Point>();

        switch (piece.Kind)
        {
            case PieceKind.Rook:
                // slides until it runs off the board or into another piece
                foreach (Point d in Orthogonal)
                {
                    Point t = piece.Tile;
                    while (true)
                    {
                        t = new Point(t.X + d.X, t.Y + d.Y);
                        if (!OnBoard(t) || Blocked(t, piece)) break;
                        moves.Add(t);
                    }
                }
                break;

            case PieceKind.Queen:
                // rook and bishop at once, which is why two is the ceiling
                foreach (Point d in AllEight)
                {
                    Point t = piece.Tile;
                    while (true)
                    {
                        t = new Point(t.X + d.X, t.Y + d.Y);
                        if (!OnBoard(t) || Blocked(t, piece)) break;
                        moves.Add(t);
                    }
                }
                break;

            case PieceKind.Knight:
                foreach (Point d in KnightSteps)
                {
                    var t = new Point(piece.Tile.X + d.X, piece.Tile.Y + d.Y);
                    if (OnBoard(t) && !Blocked(t, piece)) moves.Add(t);
                }
                break;

            case PieceKind.Pawn:
                // one square, but any direction — a forward-only pawn would be
                // dead weight the moment the soul got behind it
                foreach (Point d in AllEight)
                {
                    var t = new Point(piece.Tile.X + d.X, piece.Tile.Y + d.Y);
                    if (OnBoard(t) && !Blocked(t, piece)) moves.Add(t);
                }
                break;
        }
        return moves;
    }

    private static Point BestMove(List<Point> options, Point soul)
    {
        int best = int.MaxValue;
        var ties = new List<Point>();

        foreach (Point o in options)
        {
            int d = Math.Abs(o.X - soul.X) + Math.Abs(o.Y - soul.Y);
            if (d < best) { best = d; ties.Clear(); ties.Add(o); }
            else if (d == best) ties.Add(o);
        }

        // ties broken at random so repeated moves don't look mechanical
        return ties[Random.Shared.Next(ties.Count)];
    }

    // the squares the move crosses, so the rook can flash its whole lane
    private static void BuildPath(Point from, Point to, PieceKind kind, List<Point> into)
    {
        // sliders flash the whole lane, jumpers only the square they land on.
        // Sign handles the queen's diagonals as well as straight lines
        if (kind != PieceKind.Rook && kind != PieceKind.Queen) { into.Add(to); return; }

        int dx = Math.Sign(to.X - from.X);
        int dy = Math.Sign(to.Y - from.Y);

        Point t = from;
        while (t != to)
        {
            t = new Point(t.X + dx, t.Y + dy);
            into.Add(t);
        }
    }

    private void CommitMove(DodgeContext context)
    {
        Piece p = _pieces[_activeIndex];
        p.Tile = _moveTo;

        // it landed on the soul — that's the hit, and it stays on to keep hunting
        if (p.Tile == context.SoulTile)
        {
            context.DamagePlayer(PieceDamage);
            SoundManager.Play(ThudSound);
            context.ShakeScreen(5f, 0.2f);
        }
    }

    // only fires on the frame the soul changes tile, which is what separates
    // "you walked into it" from "it walked into you"
    private void CheckPlayerCapture(DodgeContext context)
    {
        Point soul = context.SoulTile;
        if (soul == _lastSoulTile) return;

        Point cameFrom = _lastSoulTile;
        _lastSoulTile  = soul;

        // a lone piece spends most of its cycle mid-telegraph, so leaving it
        // invincible there would make the board impossible to finish off. the
        // last one standing is always fair game
        bool lastPiece = AliveCount == 1;

        for (int i = 0; i < _pieces.Count; i++)
        {
            Piece p = _pieces[i];
            if (!p.Alive || p.Tile != soul) continue;

            // the one lining up a move can't be touched. that's what stops you
            // simply walking through the board — the piece flashing red is both
            // "this is where it's going" and "don't try it", and it still gets
            // to land its move on you
            if (!lastPiece && i == _activeIndex &&
                (_turn == Turn.Telegraph || _turn == Turn.Move)) continue;

            if (p.HurtLeft > 0f) continue; // still reeling from the last hit

            bool killed = HitPiece(p, context);

            // only a piece still standing throws you back off its square, so a
            // hit that doesn't finish the job costs you a step. taking one out
            // earns the square and you stay where you landed.
            // held rather than moved right away: the soul finishes stepping onto
            // the square and is visibly standing there before it gets thrown
            // off, and the hold stops you walking out of your own knockback
            if (!killed && OnBoard(cameFrom))
            {
                _knockbackTo   = cameFrom;
                _knockbackLeft = KnockbackDelay;
                context.HoldSoul(KnockbackDelay);
            }
            return; // one piece per square, and this one's dealt with
        }
    }

    // true if that took the piece off the board, which is what decides whether
    // the soul gets thrown back or keeps the square
    private bool HitPiece(Piece piece, DodgeContext context)
    {
        piece.Health--;

        if (piece.Health <= 0)
        {
            KillPiece(piece, context);
            return true;
        }

        piece.HurtLeft = HurtSeconds;
        SoundManager.Play(HitSound);
        SpawnBurst(context, TileCentre(piece.Tile), 5);
        return false;
    }

    private void KillPiece(Piece piece, DodgeContext context)
    {
        piece.Alive    = false;
        piece.Dying    = true;
        piece.DeathPos = TileCentre(piece.Tile);

        // flung out away from the middle so it always clears the frame
        Vector2 away = piece.DeathPos - new Vector2(_board.Center.X, _board.Center.Y);
        if (away == Vector2.Zero) away = new Vector2(1f, 0f);
        away.Normalize();

        piece.DeathVel  = away * 240f + new Vector2(0f, -200f);
        piece.DeathSpin = (Random.Shared.Next(2) == 0 ? -1f : 1f) * 9f;

        float pitch = ((float)Random.Shared.NextDouble() * 2f - 1f) * TakePitchRange;
        SoundManager.Play(TakeSound, 1f, pitch);
        SoundManager.Play(TakeLayer, 1f, pitch);

        // the green ones pay out on the way off the board
        if (piece.Heals)
        {
            context.HealPlayer(HealPercentOfMax);
            SoundManager.Play(HealSound);
        }
        SpawnBurst(context, piece.DeathPos, 10);
    }

    private void UpdateDying(float dt, DodgeContext context)
    {
        Spritesheet smoke = SpriteManager.GetSprite("smoke");
        Texture2D tex = smoke?.Texture;

        foreach (Piece p in _pieces)
        {
            if (!p.Dying) continue;

            p.DeathTimer += dt;
            p.DeathVel   += new Vector2(0f, 520f * dt); // falls away once it's clear
            p.DeathPos   += p.DeathVel * dt;

            // smoke trail behind it on the way out
            if (p.DeathTimer < 0.6f)
                context.SpawnParticle(p.DeathPos, -p.DeathVel * 0.1f, 0.45f,
                    8 + Random.Shared.Next(8), Color.White, 0.05f, tex);

            if (p.DeathTimer > 1.4f) p.Dying = false;
        }
    }

    private int AliveCount
    {
        get
        {
            int n = 0;
            foreach (Piece p in _pieces) if (p.Alive) n++;
            return n;
        }
    }

    private bool NoneAlive => AliveCount == 0;

    private bool OnBoard(Point t) => t.X >= 0 && t.Y >= 0 && t.X < Cols && t.Y < Rows;

    // another piece in the way. the soul's square is deliberately not blocked,
    // moving onto it is the whole point
    private bool Blocked(Point t, Piece self)
    {
        foreach (Piece p in _pieces)
            if (p.Alive && p != self && p.Tile == t) return true;
        return false;
    }

    // ── intro ────────────────────────────────────────────────────────────────

    private void UpdateFall()
    {
        // eases in so they read as dropping rather than sliding down
        float t = 1f - Progress(SummonSeconds);
        foreach (Piece p in _pieces)
            p.FallY = (_board.Height + PieceSrc) * t * t;
    }

    private void UpdateSquash()
    {
        float t = Progress(LandSeconds);
        float s = t < 0.45f
            ? MathHelper.Lerp(1f, SquashAmount, t / 0.45f)
            : MathHelper.Lerp(SquashAmount, 1f, (t - 0.45f) / 0.55f);

        foreach (Piece p in _pieces) p.Squash = s;
    }

    private void SpawnLandingDust(DodgeContext context)
    {
        foreach (Piece p in _pieces)
            SpawnBurst(context, TileCentre(p.Tile) + new Vector2(0f, TilePx / 2f), 6);
    }

    private static void SpawnBurst(DodgeContext context, Vector2 at, int count)
    {
        Spritesheet smoke = SpriteManager.GetSprite("smoke");
        Texture2D tex = smoke?.Texture;

        for (int i = 0; i < count; i++)
        {
            float dir = Random.Shared.Next(2) == 0 ? -1f : 1f;
            var vel = new Vector2(
                dir * (20f + (float)Random.Shared.NextDouble() * 45f),
                -15f - (float)Random.Shared.NextDouble() * 30f);

            context.SpawnParticle(at, vel, 0.55f,
                8 + Random.Shared.Next(8), Color.White, 0.05f, tex);
        }
    }

    // ── drawing ──────────────────────────────────────────────────────────────

    public void Draw(SpriteBatch sb, Texture2D pixel)
    {
        if (_phase == Phase.Expand || _phase == Phase.Done) return;

        DrawTiles(sb, pixel);
        DrawTelegraph(sb, pixel);
        DrawFrame(sb, pixel);
        DrawPieces(sb);
    }

    // only the light squares are drawn — the dark ones are the same black as the
    // background, so painting them would be a no-op
    private void DrawTiles(SpriteBatch sb, Texture2D pixel)
    {
        for (int y = 0; y < Rows; y++)
        for (int x = 0; x < Cols; x++)
        {
            if ((x + y) % 2 != 0) continue;

            float alpha = TileAlpha(x, y);
            if (alpha <= 0f) continue;

            sb.Draw(pixel, TileRect(x, y), Color.White * alpha);
        }
    }

    private float TileAlpha(int x, int y)
    {
        if (_phase != Phase.FadeIn) return 1f;

        float start = (y * Cols + x) * TileStagger;
        return MathHelper.Clamp((_timer - start) / TileFadeSeconds, 0f, 1f);
    }

    // the squares it's about to cross, pulsing. the destination burns brighter
    // than the lane so you can read where it actually stops
    private void DrawTelegraph(SpriteBatch sb, Texture2D pixel)
    {
        if (_phase != Phase.Play || _turn != Turn.Telegraph || _activeIndex < 0) return;

        float pulse = (MathF.Sin(_turnTimer * TelegraphPulseHz * MathHelper.TwoPi) + 1f) * 0.5f;

        for (int i = 0; i < _movePath.Count; i++)
        {
            bool destination = i == _movePath.Count - 1;
            float alpha = (destination ? 0.55f : 0.22f) * (0.45f + pulse * 0.55f);
            sb.Draw(pixel, TileRect(_movePath[i].X, _movePath[i].Y), TelegraphRed * alpha);
        }
    }

    private void DrawFrame(SpriteBatch sb, Texture2D pixel)
    {
        Rectangle r = _board;
        sb.Draw(pixel, new Rectangle(r.X, r.Y, r.Width, FramePx), Color.White);
        sb.Draw(pixel, new Rectangle(r.X, r.Bottom - FramePx, r.Width, FramePx), Color.White);
        sb.Draw(pixel, new Rectangle(r.X, r.Y, FramePx, r.Height), Color.White);
        sb.Draw(pixel, new Rectangle(r.Right - FramePx, r.Y, FramePx, r.Height), Color.White);
    }

    private void DrawPieces(SpriteBatch sb)
    {
        Spritesheet sheet = SpriteManager.GetSprite("chess");
        if (sheet?.Texture == null) return;

        // the ones being knocked off aren't on a square any more, they're in the
        // air spinning, so they're drawn from their own position
        for (int i = 0; i < _pieces.Count; i++)
        {
            Piece p = _pieces[i];
            if (!p.Dying) continue;

            var src = new Rectangle((int)p.Kind * PieceSrc, 0, PieceSrc, PieceSrc);
            sb.Draw(sheet.Texture, p.DeathPos, src, Color.White,
                p.DeathSpin * p.DeathTimer, new Vector2(PieceSrc / 2f), 1f, SpriteEffects.None, 0f);
        }

        // nothing on the board until they actually drop in
        if (_phase < Phase.Summon) return;

        for (int i = 0; i < _pieces.Count; i++)
        {
            Piece p = _pieces[i];
            if (!p.Alive) continue;

            var src = new Rectangle((int)p.Kind * PieceSrc, 0, PieceSrc, PieceSrc);

            // squash is anchored to the bottom of the square so it flattens down
            // into the board rather than shrinking towards its middle
            int h = (int)(PieceSrc * p.Squash);
            int w = (int)(PieceSrc * (1f + (1f - p.Squash) * 0.35f));

            Rectangle tile = TileRect(p.Tile.X, p.Tile.Y);
            var centre = new Vector2(tile.Center.X, tile.Bottom - p.FallY);

            // slides between squares on its way over, so a rook crossing the
            // board reads as travelling rather than teleporting
            if (i == _activeIndex && _turn == Turn.Move)
            {
                Rectangle from = TileRect(_moveFrom.X, _moveFrom.Y);
                Rectangle to   = TileRect(_moveTo.X, _moveTo.Y);
                float t = MathHelper.Clamp(_turnTimer / MoveSeconds, 0f, 1f);

                centre = Vector2.Lerp(
                    new Vector2(from.Center.X, from.Bottom),
                    new Vector2(to.Center.X, to.Bottom), t);
            }

            var dst = new Rectangle((int)centre.X - w / 2, (int)centre.Y - h, w, h);

            // the piece about to move burns red along with its lane
            Color tint = Color.White;
            if (i == _activeIndex && _turn == Turn.Telegraph)
            {
                float pulse = (MathF.Sin(_turnTimer * TelegraphPulseHz * MathHelper.TwoPi) + 1f) * 0.5f;
                tint = Color.Lerp(Color.White, TelegraphRed, 0.35f + pulse * 0.5f);
            }
            else if (p.HurtLeft > 0f)
            {
                // hard flicker rather than a fade, so "I hurt it but it's still
                // standing" reads differently from the telegraph's slow pulse
                tint = (int)(p.HurtLeft / 0.06f) % 2 == 0 ? TelegraphRed : Color.White;
            }

            // green ones get a 1px outline, drawn as the sprite offset in every
            // direction underneath itself. survives the red telegraph tint on
            // top, so you can still tell which one is worth chasing
            if (p.Heals)
            {
                foreach (Point o in OutlineOffsets)
                    sb.Draw(sheet.Texture,
                        new Rectangle(dst.X + o.X, dst.Y + o.Y, dst.Width, dst.Height),
                        src, HealGreen);
            }

            sb.Draw(sheet.Texture, dst, src, tint);
        }
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private void Advance(Phase next)
    {
        _phase = next;
        _timer = 0f;
    }

    private float Progress(float seconds) => MathHelper.Clamp(_timer / seconds, 0f, 1f);

    private Rectangle TileRect(int x, int y)
        => new(_origin.X + x * TilePx, _origin.Y + y * TilePx, TilePx, TilePx);

    private Vector2 TileCentre(Point tile) => new(
        _origin.X + tile.X * TilePx + TilePx / 2f,
        _origin.Y + tile.Y * TilePx + TilePx / 2f);
}
