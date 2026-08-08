using System;
using FinalProject.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using FinalProject.Battle.Dodge;
namespace FinalProject.Battle.Patterns;

// David's ultimate: he puts the song on, then keeps interrupting it with the
// rest of his lesson plan. The lyrics are the spine — they start with the turn
// and never stop — and his other three attacks are sequenced on top as beats,
// each cued off a word index rather than a timestamp so the choreography stays
// locked to the song however SpawnInterval is retuned.
//
// The beats are the real attacks, not reimplementations: HexagonPattern,
// OnionPattern and PunchPattern are constructed here in their embedded mode
// (arena and teacher stay this pattern's business, and they take a spawn count
// instead of a stretch of time) and driven through their own Start/Update/Draw.
// Retuning a hexagon or a punch in its own file carries into the ultimate for
// free — the cost is that this pattern has to own their lifetimes, which is
// what most of the bookkeeping below is for.
public class AllStarPattern : IBulletPattern
{
    // the song plays end to end, so the turn lasts as long as the lyrics plus a
    // tail for the last hazards to clear. deriving it keeps the interval below
    // as the single speed knob instead of two numbers kept in sync by hand
    private const float TailSeconds = 4f;
    public float Duration => Lyrics.Length * SpawnInterval + TailSeconds;
    
    private static readonly string[] Lyrics = new string[]
    {
        // Verse 1
        "Somebody", "once", "told", "me", "the", "world", "is", "gonna", "roll", "me",
        "I", "ain't", "the", "sharpest", "tool", "in", "the", "shed",
        "She", "was", "looking", "kind", "of", "dumb", "with", "her", "finger", "and", "her", "thumb",
        "In", "the", "shape", "of", "an", "\"L\"", "on", "her", "forehead",
        
        // Pre Chorus
        "WELL,", "the", "years", "start", "comin'", "and", "they", "don't", "stop", "comin'",
        "Fed", "to", "the", "rules", "and", "I", "hit", "the", "ground", "runnin'",
        "Didn't", "make", "sense", "not", "to", "live", "for", "fun",
        "Your", "brain", "gets", "smart", "but", "your", "head", "gets", "dumb",
        "So", "much", "to", "do,", "so", "much", "to", "see",
        "So", "what's", "wrong", "with", "taking", "the", "backstreets?",
        "You'll", "never", "know", "if", "you", "don't", "go",
        "You'll", "never", "shine", "if", "you", "don't", "glow",
        
        // Chorus
        "Hey", "now,", "you're", "an", "all", "star",
        "Get", "your", "game", "on,", "go", "play",
        "Hey", "now,", "you're", "a", "rock", "star",
        "Get", "the", "show", "on,", "get", "paid",
        "And", "all", "that", "glitters", "is", "gold",
        "Only", "shootin'", "stars", "break", "the", "mold"
    };
    
    private int _wordIndex = 0;
    private float _spawnTimer = 0f;
    private const float SpawnInterval = 0.28f; // Delay between word spawns
    private int _spawnSide = 0; // Randoms between 0: Top, 1: Left, 2: Right

    // ceiling on how much of the song is in the air at once. a word crossing
    // the widened box takes about four seconds to expire, so at the interval
    // above the stream settles at roughly fourteen live words with no cap —
    // dense enough that the beats had nowhere to read against. the cap is what
    // actually controls the clutter here, not the interval: it self-regulates
    // as the arena resizes, where a fixed rate can't
    private const int MaxLiveWords = 6;

    // ── beat scheduling ──────────────────────────────────────────────────────
    // strictly one beat at a time. a beat isn't "over" when its pattern stops
    // spawning, it's over when the last hazard it put out has left the arena —
    // otherwise the next one starts on top of rings that are still closing, and
    // stacking two of these on the lyrics is what made it unsurvivable. the
    // rest gap after each one is the breather to actually reposition in
    private const float RestSeconds      = 1.4f;
    private const float FirstBeatDelay   = 1.2f; // let the song establish itself first
    private const float LastBeatBefore   = 6f;   // no new beats this close to the end

    private enum Beat { Hexagons, Onion, Punch }

    // fixed order rather than random, so the attack teaches its own rhythm —
    // you learn what's coming next, and the difficulty is in the execution
    private static readonly Beat[] Rotation = { Beat.Hexagons, Beat.Onion, Beat.Punch };

    // the one beat pinned to the song instead of the rotation. a sky entry takes
    // Fall + LandSquash + Hop + Windup + Telegraph + Strike ≈ 1.9s to put the
    // fist down, which at the interval above is about seven words — so cueing
    // him seven words short of the chorus's last word lands the punch on the
    // word the song is named after. the scheduler holds the floor empty rather
    // than start something that would still be running when this comes due,
    // which also gives the chorus a beat of quiet right before he drops
    private const int TitlePunchCue = 103;

    // rough runtimes, only ever used to decide whether a beat fits before the
    // cue above. being a little over is fine and safer than being under — it
    // costs a held gap, where being under would collide with the reserved punch
    private static float EstimatedSeconds(Beat beat) => beat switch
    {
        Beat.Hexagons => 4.5f,
        Beat.Onion    => 4.5f,
        _             => 4.0f,
    };

    // how much of each attack a beat is worth. one punch per drop, never his
    // usual three
    private const int HexagonsPerBeat = 2;
    private const int OnionRingsPerBeat = 1;
    private const int UltimatePunches = 1;

    // arena shapes: the song plays in a widened box, and the arena tightens
    // back to the normal one for the punch so he isn't reaching across a box
    // that's wider than the geometry PunchPattern positions itself against
    private const float WidenSeconds = 0.4f;
    private const float TightenSeconds = 0.45f;
    private Rectangle _wideBox;

    // the beat currently on the floor, and the instance running it. only one is
    // ever live — this pattern owns their lifetimes, since none of them can end
    // a turn on their own any more
    private Beat? _active;
    private HexagonPattern _hex;
    private OnionPattern   _onion;
    private PunchPattern   _punch;

    private float _restTimer;
    private int   _rotationIndex;
    private bool  _titlePunchDone;

    // watchdog on the punch beat: PunchPattern parks in its Enter phase forever
    // when the teacher has no sprite rig, so Finished would never arrive and the
    // arena would stay tightened for the rest of the song
    private float _punchTime;
    
    public void Start(DodgeContext context)
    {
        // he's off stage for the song itself. the punch beat brings him back on
        // by drawing his rig directly, which is a different thing from the
        // passive floating idle this hides
        context.SetTeacherVisible(false);

        // off BaseBox rather than CurrentBox so it's a fixed shape the punch
        // beat can tighten away from and hand back afterwards
        Rectangle box = context.BaseBox;
        _wideBox = new Rectangle(
            box.X - 50,
            box.Y - 30,
            box.Width + 100,
            box.Height + 60
        );
        context.ResizeBoxTo(_wideBox, WidenSeconds);

        _wordIndex = 0;
        _spawnTimer = 0f;
        _restTimer = FirstBeatDelay;
    }

    public void Update(GameTime gameTime, DodgeContext context)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (context.Elapsed < Duration)
        {
            _spawnTimer += dt;

            // Use 'while' instead of 'if' so lag spikes don't queue up or skip timing
            while (_spawnTimer >= SpawnInterval && _wordIndex < Lyrics.Length)
            {
                _spawnTimer -= SpawnInterval;

                // over the cap the word is dropped, not delayed. the index has
                // to advance either way — it's the song's clock, and the punch
                // reservation counts against it, so stalling here would drag
                // the chorus cue out of place every time the screen got busy
                if (context.ProjectileCount < MaxLiveWords)
                    SpawnWordProjectile(context, Lyrics[_wordIndex]);

                _wordIndex++;
            }
        }

        // beats keep running past the last word — the tail in Duration is there
        // so the final hexagons and rings get to finish rather than being cut
        UpdateBeats(gameTime, context);
    }

    // run the beat on the floor, or count down to the next one. never both —
    // the whole point of the rest gap is that the arena is empty during it
    private void UpdateBeats(GameTime gameTime, DodgeContext context)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (_active.HasValue)
        {
            RunActive(gameTime, context);
            if (ActiveIsClear(context)) EndBeat(context);
            return;
        }

        _restTimer -= dt;
        if (_restTimer > 0f) return;

        TryStartBeat(context);
    }

    private void RunActive(GameTime gameTime, DodgeContext context)
    {
        switch (_active.Value)
        {
            case Beat.Hexagons: _hex.Update(gameTime, context);   break;
            case Beat.Onion:    _onion.Update(gameTime, context); break;
            case Beat.Punch:
                _punchTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
                _punch.Update(gameTime, context);
                break;
        }
    }

    // "clear" means the arena is actually empty again, not just that the
    // sub-pattern has stopped producing. hexagons are the case that matters:
    // Finished there only means the quota is out, and the edges from the last
    // one are still sliding across the box for a good second after that
    private bool ActiveIsClear(DodgeContext context) => _active.Value switch
    {
        Beat.Hexagons => _hex.Finished && context.HexCount == 0,
        Beat.Onion    => _onion.Finished,
        // the watchdog covers the no-rig case, where Finished never comes
        _             => _punch.Finished || _punchTime >= _punch.Duration + 1f,
    };

    private void EndBeat(DodgeContext context)
    {
        if (_active.Value == Beat.Punch)
        {
            _punch = null;
            context.ResizeBoxTo(_wideBox, WidenSeconds); // he's gone, give the song its stage back
        }

        _active = null;
        _restTimer = RestSeconds;
    }

    private void TryStartBeat(DodgeContext context)
    {
        // nothing new once the song is nearly out — a beat started here would
        // hold the turn open past the last word waiting for its hazards
        if (context.Elapsed > Duration - LastBeatBefore) return;

        Beat next = Rotation[_rotationIndex % Rotation.Length];

        if (!_titlePunchDone)
        {
            float toCue = (TitlePunchCue - _wordIndex) * SpawnInterval;

            if (toCue <= 0f)
            {
                _titlePunchDone = true;

                // steps over the rotation's own punch if that's what was up
                // next, so the reserved one doesn't get followed straight away
                // by a second identical drop
                if (next == Beat.Punch) _rotationIndex++;

                StartBeat(Beat.Punch, context);
                return;
            }

            // wouldn't finish in time. hold the floor empty rather than let it
            // run into the reserved punch
            if (toCue < EstimatedSeconds(next) + RestSeconds) return;
        }

        _rotationIndex++;
        StartBeat(next, context);
    }

    // everything is built with ownsArena: false, which is what keeps the
    // sub-patterns from resizing the box or putting the passive teacher idle
    // back on screen mid-song
    private void StartBeat(Beat beat, DodgeContext context)
    {
        switch (beat)
        {
            case Beat.Hexagons:
                _hex = new HexagonPattern(ownsArena: false, maxSpawns: HexagonsPerBeat, colorCoded: true);
                _hex.Start(context);
                break;

            case Beat.Onion:
                _onion = new OnionPattern(ownsArena: false, maxRings: OnionRingsPerBeat);
                _onion.Start(context);
                break;

            case Beat.Punch:
                _punch = new PunchPattern(UltimatePunches, entersFromSky: true);
                _punchTime = 0f;
                _punch.Start(context);

                // tighten while he's still falling, so the arena has finished
                // moving by the time he lands in it. PunchPattern places itself
                // against BaseBox, so without this he reaches across a box that
                // isn't there
                context.ResizeBoxTo(context.BaseBox, TightenSeconds);
                break;
        }

        _active = beat;
    }

    private void SpawnWordProjectile(DodgeContext context, string word)
    {
        Rectangle box = context.CurrentBox;
        Vector2 spawnPos;
        Vector2 velocity;

        // Changable speed
        float speed = GameSettings.DodgeProjectileSpeed * 0.9f;

        // Change sides at random
        switch (_spawnSide)
        {
            case 0: // Top
                float randomX = Random.Shared.Next(box.Left + 10, box.Right - 30);
                spawnPos = new Vector2(randomX, box.Top - 5); 
                velocity = new Vector2(0f, speed);
                break;

            case 1: // Left
                float randomYLeft = Random.Shared.Next(box.Top + 10, box.Bottom - 10);
                spawnPos = new Vector2(box.Left - 5, randomYLeft); 
                velocity = new Vector2(speed * 0.9f, 0f);
                break;

            case 2: // Right
            default:
                float randomYRight = Random.Shared.Next(box.Top + 10, box.Bottom - 10);
                spawnPos = new Vector2(box.Right + 5, randomYRight); 
                velocity = new Vector2(-speed * 0.9f, 0f);
                break;
        }

        // Cycle spawn side for next word
        _spawnSide = (_spawnSide + 1) % 3;

        // null font: DodgePhase substitutes the one it draws with, so the word
        // is measured with the same font it's rendered from. passing a font
        // cached in a Draw call meant the first words were sized by
        // Projectile's character-count fallback while being drawn for real
        context.SpawnProjectile(spawnPos, velocity, word, null, 2f);
    }
    
    // his body, drawn in the same slot PunchPattern gets standalone — behind the
    // box border and the bullets, so the lyrics stay readable across him.
    // called from DodgePhase.Draw, which is the only place the font exists
    public void DrawBehind(SpriteBatch spriteBatch, Texture2D pixel, SpriteFont font)
    {
        _punch?.Draw(spriteBatch, pixel, font);
    }

    // the rings go in front of the words instead — they're the thing you have
    // to read a gap in, and a lyric crossing one shouldn't hide where it is
    public void Draw(SpriteBatch spriteBatch, Texture2D pixel, DodgeContext context)
    {
        if (_active == Beat.Onion) _onion.Draw(spriteBatch, pixel, context);
    }
}