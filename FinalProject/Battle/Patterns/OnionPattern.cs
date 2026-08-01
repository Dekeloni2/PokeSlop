using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FinalProject.Battle.Patterns;

// Also runs as one beat inside David's ultimate — see AllStarPattern. In that
// mode the host owns the arena and the teacher, and asks for a fixed number of
// layers rather than a stretch of time. The host keeps calling Update after the
// last one is spawned so the rings still close in and clear on their own.
public class OnionPattern : IBulletPattern
{
    public float Duration => 15f;

    private readonly List<OnionRing> _rings = new();
    private float _spawnTimer;
    private float _spawnInterval = 1.8f; // Time between new onion layers
    private float _ringSpeed = 60f;      // How fast layers shrink inward
    private Random _rnd = new();

    private readonly bool _ownsArena; // false when a host pattern is driving it
    private readonly int  _maxRings;  // 0 = keep layering for the full Duration
    private int _spawnedRings;

    // so a host can tell whether there's still something live out there
    public bool HasRings => _rings.Count > 0;

    public OnionPattern(bool ownsArena = true, int maxRings = 0)
    {
        _ownsArena = ownsArena;
        _maxRings  = maxRings;
    }

    // unlike the hexagons, the rings are drawn and collided by this pattern, so
    // a host has to keep it alive until the last one has closed — not just
    // until the last one has spawned
    public bool Finished => _maxRings > 0 && _spawnedRings >= _maxRings && _rings.Count == 0;

    public void Start(DodgeContext context)
    {
        _rings.Clear();
        _spawnTimer   = 0f;
        _spawnedRings = 0;

        if (!_ownsArena)
        {
            // the host already shaped the arena — open with a ring straight
            // away instead of waiting out a full interval first
            _spawnTimer = _spawnInterval;
            return;
        }

        // Standard square battle box for ring patterns
        Rectangle box = new Rectangle(
            context.BaseBox.X, context.BaseBox.Y,
            context.BaseBox.Width, context.BaseBox.Height);

        context.ResizeBoxTo(box, 0.5f);

        context.SetTeacherVisible(true);
    }

    public void Update(GameTime gameTime, DodgeContext context)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _spawnTimer += dt;

        // spawn new "layers"
        bool canSpawn = _maxRings <= 0 || _spawnedRings < _maxRings;
        if (canSpawn && _spawnTimer >= _spawnInterval)
        {
            _spawnTimer = 0f;
            _spawnedRings++;

            // max radius based on the box dimensions
            float startRadius = context.CurrentBox.Width * 0.7f;
                
            // random angle for the gap/gate (in radians)
            float randomGateAngle = (float)(_rnd.NextDouble() * Math.PI * 2);
                
            // gate opening size (approx 45 degrees wide)
            float gateWidth = MathHelper.ToRadians(45f);

            _rings.Add(new OnionRing(startRadius, randomGateAngle, gateWidth, _ringSpeed));
        }

        // update existing rings
        Vector2 boxCenter = new Vector2(context.CurrentBox.Center.X, context.CurrentBox.Center.Y);

        for (int i = _rings.Count - 1; i >= 0; i--)
        {
            var ring = _rings[i];
            ring.Update(dt);

            // collision check against player
            if (!ring.HasHitPlayer && ring.CheckCollision(boxCenter, context.HitboxPosition))
            {
                ring.HasHitPlayer = true;
                context.DamagePlayer(3);
            }

            if (ring.IsExpired)
            {
                _rings.RemoveAt(i);
            }
        }
    }
    
    public void Draw(SpriteBatch spriteBatch, Texture2D pixel, DodgeContext context)
    {
        Vector2 center = new Vector2(context.CurrentBox.Center.X, context.CurrentBox.Center.Y);

        foreach (var ring in _rings)
        {
            DrawOnionRing(spriteBatch, pixel, center, ring);
        }
    }

    private void DrawOnionRing(SpriteBatch spriteBatch, Texture2D pixel, Vector2 center, OnionRing ring)
    {
        int segments = 60; // how smooth the circle looks
        float step = MathHelper.TwoPi / segments;
        float halfGate = ring.GateWidth / 2f;

        for (int i = 0; i < segments; i++)
        {
            float a1 = i * step;
            float a2 = (i + 1) * step;

            // skip drawing line segments that fall inside the gate opening
            float diff1 = MathHelper.WrapAngle(a1 - ring.GateAngle);
            float diff2 = MathHelper.WrapAngle(a2 - ring.GateAngle);

            if (Math.Abs(diff1) < halfGate || Math.Abs(diff2) < halfGate)
                continue;

            // calculate line segment start & end points
            Vector2 p1 = center + new Vector2((float)Math.Cos(a1), (float)Math.Sin(a1)) * ring.Radius;
            Vector2 p2 = center + new Vector2((float)Math.Cos(a2), (float)Math.Sin(a2)) * ring.Radius;

            DrawLine(spriteBatch, pixel, p1, p2, Color.White, 4);
        }
    }

    // simple line rendering helper using a 1x1 pixel texture
    private void DrawLine(SpriteBatch spriteBatch, Texture2D pixel, Vector2 start, Vector2 end, Color color, int thickness)
    {
        Vector2 edge = end - start;
        float angle = (float)Math.Atan2(edge.Y, edge.X);

        spriteBatch.Draw(
            pixel,
            start,
            null,
            color,
            angle,
            Vector2.Zero,
            new Vector2(edge.Length(), thickness),
            SpriteEffects.None,
            0f
        );
    }
}