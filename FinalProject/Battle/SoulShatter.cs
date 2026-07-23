using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FinalProject.Core.Audio;
using FinalProject.Core.Graphics;

namespace FinalProject.Battle
{
    // The Undertale death effect. Frame 0 of the soul_lost sheet (the soul cut
    // in half) holds for a beat, then it bursts into its 6 shards (frames 1-6)
    // which fly outward, spin, and fall under gravity. Drawn in screen space.
    public class SoulShatter
    {
        private const float BreakSeconds = 1.0f;  // "broken in half" frame holds, then it shatters
        private const float ShardSeconds = 1.6f;  // shards fly for this long, then done
        private const float ShardScale   = 1f;    // native size — the live soul draws 1:1
        private const float Gravity      = 340f;  // px/s^2
        private const float MinSpeed     = 130f;
        private const float SpeedRange   = 130f;
        private const float SpinRange    = 9f;     // rad/s

        // the soul_lost sheet is NOT a uniform grid — these are the measured
        // per-frame rects (frame 0 is the whole soul cut in half, 1-6 the shards)
        private static readonly Rectangle[] Frames =
        {
            new Rectangle(0,  0, 16, 16),
            new Rectangle(17, 0, 7,  16),
            new Rectangle(25, 0, 4,  16),
            new Rectangle(30, 0, 7,  16),
            new Rectangle(38, 0, 7,  16),
            new Rectangle(46, 0, 5,  16),
            new Rectangle(52, 0, 7,  16),
        };

        private struct Shard
        {
            public int     Frame;
            public Vector2 Pos;
            public Vector2 Vel;
            public float   Angle;
            public float   Spin;
        }

        private readonly Vector2 _center;
        private readonly Shard[] _shards = new Shard[6];
        private float _timer;
        private bool  _burst;

        public bool IsFinished { get; private set; }

        public SoulShatter(Vector2 center)
        {
            _center = center;
            SoundManager.Play("snd_break1"); // the crack, the moment it breaks
        }

        public void Update(float dt)
        {
            _timer += dt;

            if (!_burst)
            {
                if (_timer >= BreakSeconds)
                {
                    _burst = true;
                    _timer = 0f;
                    SpawnShards();
                    SoundManager.Play("snd_break2"); // the shatter into shards
                }
                return;
            }

            for (int i = 0; i < _shards.Length; i++)
            {
                _shards[i].Vel.Y += Gravity * dt;
                _shards[i].Pos   += _shards[i].Vel * dt;
                _shards[i].Angle += _shards[i].Spin * dt;
            }

            if (_timer >= ShardSeconds) IsFinished = true;
        }

        private void SpawnShards()
        {
            for (int i = 0; i < 6; i++)
            {
                // fan the shards outward around a circle, with a little jitter
                float angle = i * MathHelper.TwoPi / 6f
                              + ((float)Random.Shared.NextDouble() - 0.5f) * 0.5f;
                float speed = MinSpeed + (float)Random.Shared.NextDouble() * SpeedRange;
                _shards[i] = new Shard
                {
                    Frame = i + 1,
                    Pos   = _center,
                    Vel   = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * speed,
                    Angle = 0f,
                    Spin  = ((float)Random.Shared.NextDouble() * 2f - 1f) * SpinRange,
                };
            }
        }

        public void Draw(SpriteBatch sb, Spritesheet sheet)
        {
            if (sheet == null) return;

            if (!_burst)
            {
                DrawFrame(sb, sheet.Texture, 0, _center, 0f); // the soul, cut in half
            }
            else
            {
                for (int i = 0; i < _shards.Length; i++)
                    DrawFrame(sb, sheet.Texture, _shards[i].Frame, _shards[i].Pos, _shards[i].Angle);
            }
        }

        private static void DrawFrame(SpriteBatch sb, Texture2D texture, int frame, Vector2 center, float angle)
        {
            Rectangle src = Frames[frame];
            var origin = new Vector2(src.Width / 2f, src.Height / 2f);
            sb.Draw(texture, center, src, Color.White, angle, origin, ShardScale, SpriteEffects.None, 0f);
        }
    }
}
