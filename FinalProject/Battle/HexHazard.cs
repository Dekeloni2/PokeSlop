using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FinalProject.Battle
{
    // An expanding hexagon hazard. Only its 6 edges hurt (continuous damage) —
    // the interior is safe, so it can grow right over the player. It fades in
    // (a brief non-damaging telegraph), grows to a max radius, then explodes:
    // each edge detaches and slides straight out along its normal until it
    // leaves the box. DodgePhase owns the list and applies the damage.
    public class HexHazard
    {
        public const  int   Damage         = 1;
        private const float DamageInterval = 0.2f;  // seconds between damage ticks
        private const float FadeSeconds    = 0.35f; // telegraph — visible but harmless
        private const float LineThickness  = 4f;

        private enum Phase { FadeIn, Grow, Explode, Done }

        private readonly Vector2 _center;
        private readonly float   _rotation;
        private readonly float   _startRadius;
        private readonly float   _maxRadius;
        private readonly float   _growSeconds;
        private readonly float   _explodeSpeed;

        private Phase _phase = Phase.FadeIn;
        private float _timer;
        private float _radius;
        private float _explodeDist;
        private float _damageCooldown;

        public bool IsFinished => _phase == Phase.Done;

        public HexHazard(Vector2 center, float startRadius, float maxRadius,
            float rotation, float growSeconds, float explodeSpeed)
        {
            _center       = center;
            _startRadius  = startRadius;
            _maxRadius    = maxRadius;
            _rotation     = rotation;
            _growSeconds  = growSeconds;
            _explodeSpeed = explodeSpeed;
            _radius       = startRadius;
        }

        public void Update(float dt, Rectangle box)
        {
            _timer          += dt;
            _damageCooldown -= dt;

            switch (_phase)
            {
                case Phase.FadeIn:
                    if (_timer >= FadeSeconds) { _phase = Phase.Grow; _timer = 0f; }
                    break;

                case Phase.Grow:
                    float t = MathHelper.Clamp(_timer / _growSeconds, 0f, 1f);
                    _radius = MathHelper.Lerp(_startRadius, _maxRadius, t);
                    if (t >= 1f) { _phase = Phase.Explode; _timer = 0f; }
                    break;

                case Phase.Explode:
                    _explodeDist += _explodeSpeed * dt;
                    if (AllEdgesOutside(box) || _timer > 4f)
                        _phase = Phase.Done;
                    break;
            }
        }

        // returns true on the frames the player should take a damage tick. The
        // fade-in is a harmless telegraph; grow and explode both hurt.
        public bool TickDamage(Rectangle hitbox)
        {
            if (_phase == Phase.FadeIn || _phase == Phase.Done) return false;
            if (_damageCooldown > 0f) return false;

            Vector2 p = hitbox.Center.ToVector2();
            float reach = LineThickness / 2f + Math.Max(hitbox.Width, hitbox.Height) / 2f;

            for (int i = 0; i < 6; i++)
            {
                (Vector2 a, Vector2 b) = Edge(i);
                if (DistToSegment(p, a, b) <= reach)
                {
                    _damageCooldown = DamageInterval;
                    return true;
                }
            }
            return false;
        }

        public void Draw(SpriteBatch sb, Texture2D pixel)
        {
            float alpha = _phase == Phase.FadeIn
                ? MathHelper.Clamp(_timer / FadeSeconds, 0f, 1f)
                : 1f;
            Color color = Color.White * alpha;

            for (int i = 0; i < 6; i++)
            {
                (Vector2 a, Vector2 b) = Edge(i);
                DrawLine(sb, pixel, a, b, LineThickness, color);
            }
        }

        // ── geometry ─────────────────────────────────────────────────────────

        private Vector2 Vertex(int k)
        {
            float ang = _rotation + k * MathHelper.TwoPi / 6f;
            return _center + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * _radius;
        }

        // edge i as its two endpoints. Once exploding, the whole segment is
        // pushed outward along its normal so it slides straight out, keeping
        // its length and angle.
        private (Vector2, Vector2) Edge(int i)
        {
            Vector2 a = Vertex(i);
            Vector2 b = Vertex((i + 1) % 6);

            if (_phase == Phase.Explode && _explodeDist > 0f)
            {
                Vector2 n = (a + b) / 2f - _center;
                if (n != Vector2.Zero) n.Normalize();
                Vector2 off = n * _explodeDist;
                a += off;
                b += off;
            }
            return (a, b);
        }

        private bool AllEdgesOutside(Rectangle box)
        {
            var bounds = new Rectangle(box.X - 32, box.Y - 32, box.Width + 64, box.Height + 64);
            for (int i = 0; i < 6; i++)
            {
                (Vector2 a, Vector2 b) = Edge(i);
                if (bounds.Contains(a.ToPoint()) || bounds.Contains(b.ToPoint()))
                    return false;
            }
            return true;
        }

        // ── helpers ──────────────────────────────────────────────────────────

        private static float DistToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float lenSq = ab.LengthSquared();
            if (lenSq < 0.0001f) return Vector2.Distance(p, a);
            float t = MathHelper.Clamp(Vector2.Dot(p - a, ab) / lenSq, 0f, 1f);
            return Vector2.Distance(p, a + ab * t);
        }

        private static void DrawLine(SpriteBatch sb, Texture2D pixel, Vector2 a, Vector2 b, float thickness, Color color)
        {
            Vector2 delta = b - a;
            float length = delta.Length();
            float angle  = MathF.Atan2(delta.Y, delta.X);
            sb.Draw(pixel, a, null, color, angle,
                new Vector2(0f, 0.5f), new Vector2(length, thickness), SpriteEffects.None, 0f);
        }
    }
}
