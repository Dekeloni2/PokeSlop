using System;
using Microsoft.Xna.Framework;

namespace FinalProject.Battle.Patterns;

// The hexagon attack. The arena widens, then hexagons fade in at random spots,
// grow, briefly charge (shake + red pulse), then explode into 6 edges that
// slide outward. Only the edges hurt, so a hexagon can grow right over you.
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

    public void Start(DodgeContext context)
    {
        // widen the arena for this attack
        Rectangle b = context.BaseBox;
        int newW = (int)(b.Width * BoxWidthScale);
        var expanded = new Rectangle(b.Center.X - newW / 2, b.Y, newW, b.Height);
        context.ResizeBoxTo(expanded, ExpandSeconds);

        _spawnTimer = 0f;
    }

    public void Update(GameTime gameTime, DodgeContext context)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // wait for the widen to finish before the first hexagon, and stop
        // spawning near the end so the last ones can explode and clear
        bool spawning = context.Elapsed >= ExpandSeconds
                        && context.Elapsed < Duration - StopSpawningBefore;
        if (!spawning) return;

        _spawnTimer += dt;
        if (_spawnTimer >= SpawnInterval && context.HexCount < MaxConcurrent)
        {
            _spawnTimer = 0f;
            SpawnHex(context);
        }
    }

    private void SpawnHex(DodgeContext context)
    {
        Rectangle box = context.CurrentBox;

        Vector2 center = Random.Shared.NextDouble() < OnPlayerChance
            ? context.HitboxPosition
            : new Vector2(box.Left + Random.Shared.Next(box.Width),
                          box.Top  + Random.Shared.Next(box.Height));

        float maxRadius = MaxRadiusFrac * box.Height;
        context.SpawnHex(center, StartRadius, maxRadius, 0f, GrowSeconds, ExplodeSpeed);
    }
}
