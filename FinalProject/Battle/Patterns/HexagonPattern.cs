using System;
using Microsoft.Xna.Framework;

using FinalProject.Battle.Dodge;
namespace FinalProject.Battle.Patterns;

// The hexagon attack. The arena widens, then hexagons fade in at random spots,
// grow, briefly charge (shake + red pulse), then explode into 6 edges that
// slide outward. Only the edges hurt, so a hexagon can grow right over you.
//
// Also runs as one beat inside David's ultimate — see AllStarPattern. In that
// mode the host already owns the arena and the teacher, and hands over a fixed
// number of hexagons instead of a stretch of time, so this stops widening the
// box and counts spawns rather than watching the clock.
public class HexagonPattern : IBulletPattern
{
    public float Duration => 12f;

    private const float ExpandSeconds     = 0.6f; // arena-widen transition
    private const float BoxWidthScale     = 2f;   // arena gets this much wider
    private const float SpawnInterval      = 1.1f; // time between hexagons
    private const int   MaxConcurrent      = 3;
    private const float OnPlayerChance     = 0.0f; // rest spawn at random
    private const float StopSpawningBefore = 3f;   // let the last hexes finish

    // per-hexagon tuning
    private const float StartRadius   = 24f;
    private const float MaxRadiusFrac = 0.4f;  // of the box height
    private const float GrowSeconds   = 1.6f;
    private const float ExplodeSpeed  = 220f;

    private float _spawnTimer;

    // its own clock rather than context.Elapsed — as a beat this starts partway
    // into the turn, so the turn timer says nothing about how far along it is
    private float _elapsed;

    private readonly bool _ownsArena; // false when a host pattern is driving it
    private readonly int  _maxSpawns; // 0 = keep going for the full Duration
    private int _spawned;

    // when set, every hexagon is blue or orange and plays by that rule instead
    // of always hurting. off by default, so the standalone attack stays the
    // plain white "just don't touch the edges" lesson it teaches on its own
    private readonly bool _colorCoded;

    public HexagonPattern(bool ownsArena = true, int maxSpawns = 0, bool colorCoded = false)
    {
        _ownsArena  = ownsArena;
        _maxSpawns  = maxSpawns;
        _colorCoded = colorCoded;
    }

    // a capped run is over once its quota is out. the hexagons already in the
    // air belong to DodgePhase and finish on their own, so a host can drop this
    // the moment it goes true and start the next burst over the top
    public bool Finished => _maxSpawns > 0 && _spawned >= _maxSpawns;

    public void Start(DodgeContext context)
    {
        _spawnTimer = 0f;
        _elapsed    = 0f;
        _spawned    = 0;

        if (!_ownsArena)
        {
            // the host set the stage already. there's no widen to sit through,
            // so the first hexagon is due immediately — a beat that opens with
            // a second of nothing doesn't read as a beat
            _spawnTimer = SpawnInterval;
            return;
        }

        // widen the arena for this attack
        Rectangle b = context.BaseBox;
        int newW = (int)(b.Width * BoxWidthScale);
        var expanded = new Rectangle(b.Center.X - newW / 2, b.Y, newW, b.Height);
        context.ResizeBoxTo(expanded, ExpandSeconds);

        // he stands over the widened arena and drops the hexagons himself
        context.SetTeacherVisible(true);
    }

    public void Update(GameTime gameTime, DodgeContext context)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _elapsed += dt;

        if (_maxSpawns > 0 && _spawned >= _maxSpawns) return;

        // wait for the widen to finish before the first hexagon, and stop
        // spawning near the end so the last ones can explode and clear. a
        // capped run has neither problem — the host picks the window, and the
        // count is what ends it
        bool spawning = _maxSpawns > 0
            || (_elapsed >= ExpandSeconds && _elapsed < Duration - StopSpawningBefore);
        if (!spawning) return;

        _spawnTimer += dt;
        if (_spawnTimer >= SpawnInterval && context.HexCount < MaxConcurrent)
        {
            _spawnTimer = 0f;
            SpawnHex(context);
            _spawned++;
        }
    }

    private void SpawnHex(DodgeContext context)
    {
        Rectangle box = context.CurrentBox;

        Vector2 center = Random.Shared.NextDouble() < OnPlayerChance
            ? context.HitboxPosition
            : new Vector2(box.Left + Random.Shared.Next(box.Width),
                          box.Top  + Random.Shared.Next(box.Height));

        // rolled per hexagon rather than per burst, so two on screen together
        // can disagree and the player has to read each one instead of learning
        // the answer once and coasting
        HazardRule rule = _colorCoded
            ? (Random.Shared.Next(2) == 0 ? HazardRule.Blue : HazardRule.Orange)
            : HazardRule.White;

        float maxRadius = MaxRadiusFrac * box.Height;
        context.SpawnHex(center, StartRadius, maxRadius, 0f, GrowSeconds, ExplodeSpeed, rule);
    }
}
