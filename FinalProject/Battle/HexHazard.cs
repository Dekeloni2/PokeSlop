using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FinalProject.Core.Audio;

namespace FinalProject.Battle
{
    // An expanding hexagon hazard. Only its 6 edges hurt (continuous damage) —
    // the interior is safe, so it can grow right over the player. It fades in
    // (a brief non-damaging telegraph), grows to a max radius, then explodes:
    // each edge detaches and slides straight out along its normal until it
    // leaves the box. DodgePhase owns the list and applies the damage.
    //
    // Several of these can be alive at once, each on its own timeline, so the
    // sound for each stage lives here rather than in HexagonPattern — the
    // pattern only ever sees an aggregate HexCount, not which hex is doing
    // what, so it has no way to know when any one of them changes phase.
    public class HexHazard
    {
        public const  int   Damage         = 6;
        private const float DamageInterval = 0.2f;  // seconds between damage ticks
        private const float FadeSeconds    = 0.35f; // telegraph — visible but harmless
        private const float LineThickness  = 4f;

        // charge is the warning before it blows. at full size it holds for a bit,
        // shaking harder and going redder as it builds up
        private const float ChargeSeconds  = 0.5f;  // how long the wind-up lasts
        private const float ShakeMagnitude = 3f;    // px of jitter at the peak of the charge
        private const float PulseHz        = 8f;    // red-pulse cycles per second

        // spawn: the same "something's emerging" cue Cloverbyte's tongue and
        // David's punch telegraph use, so the vocabulary carries across
        // teachers instead of every attack inventing its own "pay attention" sound
        private const string SpawnSound = "snd_spearrise";
        // charge: this is the one warning the edges are about to go live, so
        // it needs to read as clearly distinct from the spawn cue above
        private const string ChargeSound = "snd_screenshake";
        // explode: the same weight Cloverbyte's sweep and the punch's strike
        // use, this is the moment the hex actually becomes what it's for
        private const string ExplodeSound = "snd_heavydamage";

        private enum Phase { FadeIn, Grow, Charge, Explode, Done }

        private readonly Vector2 _center;
        private readonly float   _rotation;
        private readonly float   _startRadius;
        private readonly float   _maxRadius;
        private readonly float   _growSeconds;
        private readonly float      _explodeSpeed;
        private readonly HazardRule _rule;

        private Phase   _phase = Phase.FadeIn;
        private float   _timer;
        private float   _radius;
        private float   _explodeDist;
        private float   _damageCooldown;
        private Vector2 _shakeOffset; // nonzero only during Charge; shifts the whole hex

        public bool IsFinished => _phase == Phase.Done;

        public HexHazard(Vector2 center, float startRadius, float maxRadius,
            float rotation, float growSeconds, float explodeSpeed, HazardRule rule = HazardRule.White)
        {
            _center       = center;
            _startRadius  = startRadius;
            _maxRadius    = maxRadius;
            _rotation     = rotation;
            _growSeconds  = growSeconds;
            _explodeSpeed = explodeSpeed;
            _rule         = rule;
            _radius       = startRadius;

            // FadeIn starts the instant this exists, so the spawn cue plays
            // right here rather than waiting for the first Update
            SoundManager.Play(SpawnSound);
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
                    if (t >= 1f)
                    {
                        _phase = Phase.Charge;
                        _timer = 0f;
                        SoundManager.Play(ChargeSound);
                    }
                    break;

                case Phase.Charge:
                    // jitter builds up from nothing over the windup so the shake
                    // gets worse the closer it is to exploding
                    float charge = MathHelper.Clamp(_timer / ChargeSeconds, 0f, 1f);
                    float mag    = ShakeMagnitude * charge;
                    _shakeOffset = new Vector2(
                        ((float)Random.Shared.NextDouble() * 2f - 1f) * mag,
                        ((float)Random.Shared.NextDouble() * 2f - 1f) * mag);

                    if (_timer >= ChargeSeconds)
                    {
                        _phase       = Phase.Explode;
                        _timer       = 0f;
                        _shakeOffset = Vector2.Zero;
                        SoundManager.Play(ExplodeSound);
                    }
                    break;

                case Phase.Explode:
                    _explodeDist += _explodeSpeed * dt;
                    if (AllEdgesOutside(box) || _timer > 4f)
                        _phase = Phase.Done;
                    break;
            }
        }

        // returns true on the frames the player should take a damage tick. only
        // the fade in is harmless, grow/charge/explode all hurt. the edges are
        // solid the whole time, the charge is just a visual warning
        public bool TickDamage(Rectangle hitbox, bool playerMoving)
        {
            if (_phase == Phase.FadeIn || _phase == Phase.Done) return false;

            // checked before the cooldown on purpose: a rule the player is
            // currently answering correctly shouldn't burn the cooldown, or
            // standing still in a blue edge would eat a tick the moment they
            // moved again rather than starting a fresh interval
            if (!_rule.Connects(playerMoving)) return false;
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

        // what the hex is drawn in when it isn't doing anything special. a ruled
        // hex is its rule colour from the very first frame of the fade in, so
        // there's never a moment where it's on screen without saying which one
        // it is
        private Color BaseColor => _rule.Tint();

        public void Draw(SpriteBatch sb, Texture2D pixel)
        {
            Color color;
            if (_phase == Phase.FadeIn)
            {
                // harmless telegraph fading in
                color = BaseColor * MathHelper.Clamp(_timer / FadeSeconds, 0f, 1f);
            }
            else if (_phase == Phase.Charge)
            {
                // pulse, deeper as the charge builds. goes with the shake.
                // a plain hex goes red, the codebase's "about to go off" colour —
                // but a ruled one pulses to white instead, because red over blue
                // or orange muddies the one thing the player has to read. the
                // flash still reads as a warning, the hue survives it
                float charge = MathHelper.Clamp(_timer / ChargeSeconds, 0f, 1f);
                float pulse  = (MathF.Sin(_timer * PulseHz * MathHelper.TwoPi) + 1f) * 0.5f;
                Color peak   = _rule == HazardRule.White ? Color.Red : Color.White;
                color = Color.Lerp(BaseColor, peak, pulse * charge);
            }
            else
            {
                color = BaseColor;
            }

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
            // _shakeOffset moves the whole hex while charging so the drawn edges
            // and the damage check stay on the same spot while it shakes
            return _center + _shakeOffset + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * _radius;
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
