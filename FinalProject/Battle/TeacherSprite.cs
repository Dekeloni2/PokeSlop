using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FinalProject.Core.Graphics;
using FinalProject.Data;

namespace FinalProject.Battle
{
    // draws a teacher from its separate parts and drifts each one on a sine wave.
    // nothing here is hand animated, the movement comes from every part having
    // its own speed and phase so they go out of sync with each other
    public class TeacherSprite
    {
        // how long the hurt face + shake last after taking a hit
        private const float HurtSeconds   = 0.35f;
        private const float ShakeStrength = 3f; // px

        private readonly TeacherSpriteData _data;
        private readonly Spritesheet       _sheet;

        private float _time;
        private float _hurtLeft;

        // assembled size in screen px, worked out from the parts and scaled.
        // BattleState uses it to centre him over the box
        public Vector2 Size { get; }

        public TeacherSprite(TeacherSpriteData data)
        {
            _data  = data;
            _sheet = data != null ? SpriteManager.GetSprite(data.Sheet) : null;

            if (data == null || data.Parts.Count == 0) return;

            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;

            foreach (TeacherPartData part in data.Parts)
            {
                minX = MathF.Min(minX, part.Offset.X);
                minY = MathF.Min(minY, part.Offset.Y);
                maxX = MathF.Max(maxX, part.Offset.X + part.Src.Width);
                maxY = MathF.Max(maxY, part.Offset.Y + part.Src.Height);
            }

            Size = new Vector2(maxX - minX, maxY - minY) * data.Scale;
        }

        // false if there's no sprite data or the sheet isn't registered, so the
        // battle just runs without a teacher on screen
        public bool IsReady => _data != null && _sheet != null && _data.Parts.Count > 0;

        private bool IsHurt => _hurtLeft > 0f;

        // called when the teacher takes damage, swaps to the hurt face and
        // shakes the whole body for a moment
        public void Hurt() => _hurtLeft = HurtSeconds;

        public void Update(float dt)
        {
            _time += dt;
            if (_hurtLeft > 0f) _hurtLeft -= dt;
        }

        public void Draw(SpriteBatch spriteBatch, Vector2 anchor)
        {
            if (!IsReady) return;

            // the whole character jitters while hurt, dies down as it wears off
            Vector2 shake = Vector2.Zero;
            if (IsHurt)
            {
                float mag = ShakeStrength * (_hurtLeft / HurtSeconds);
                shake = new Vector2(
                    ((float)Random.Shared.NextDouble() * 2f - 1f) * mag,
                    ((float)Random.Shared.NextDouble() * 2f - 1f) * mag);
            }

            float s = _data.Scale;

            foreach (TeacherPartData part in _data.Parts)
            {
                // Speed is cycles per second, Phase is 0..1 of a cycle
                float wave = MathF.Sin((_time * part.Speed + part.Phase) * MathHelper.TwoPi);

                // offsets and bob scale with the art so the parts stay together
                Vector2 pos = anchor + _data.Anchor + shake
                            + (part.Offset + new Vector2(wave * part.BobX, wave * part.BobY)) * s;

                Rectangle src = IsHurt && part.SrcHurt.HasValue ? part.SrcHurt.Value : part.Src;

                spriteBatch.Draw(
                    _sheet.Texture,
                    new Rectangle((int)pos.X, (int)pos.Y, (int)(src.Width * s), (int)(src.Height * s)),
                    src,
                    Color.White);
            }
        }
    }
}
