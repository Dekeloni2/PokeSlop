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
        // side to side wobble when hit. the killing blow shakes him harder and
        // for longer, and BattleState waits for it before his last words
        private const float HurtSeconds   = 0.35f;
        private const float HurtShake     = 3f;  // px
        private const float DeathSeconds  = 0.9f;
        private const float DeathShake    = 9f;  // px
        private const float ShakeHz       = 14f; // wobbles per second

        private readonly TeacherSpriteData _data;
        private readonly Spritesheet       _sheet;

        // defeated: the parts stop drifting and crumble downward while fading.
        // spared: everything freezes where it is and just goes translucent
        // the sprite gets sliced into horizontal bands that scatter sideways and
        // drift up as they fade, starting at the head and working down, so he
        // disintegrates instead of just dimming
        private const float DustSeconds  = 1.714f; // matches snd_vaporized.wav
        private const int   BandHeight   = 2;    // source px per slice
        private const float DustSpread   = 34f;  // px a slice can slide sideways
        private const float DustRise     = 26f;  // px slices drift upward
        private const float TopDownDelay = 0.55f; // how much later the feet start
        private const float SparedAlpha  = 0.35f;

        private float _time;
        private float _hurtLeft;
        private float _hurtSpan;
        private float _shakeStrength;
        private float _dustLeft;
        private bool  _dusting;
        private bool  _spared;
        private bool  _frozen;

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
        // fatal is the killing blow, a bigger and longer wobble
        public void Hurt(bool fatal = false)
        {
            _hurtLeft      = fatal ? DeathSeconds : HurtSeconds;
            _hurtSpan      = _hurtLeft;
            _shakeStrength = fatal ? DeathShake : HurtShake;
        }

        // true while the wobble is still going
        public bool IsShaking => _hurtLeft > 0f;

        // starts the crumble. IsDustFinished tells BattleState when it's over
        public void Dust()
        {
            _dusting  = true;
            _dustLeft = DustSeconds;
            _hurtLeft = 0f;
        }

        // freezes him mid pose and makes him see through
        public void Spare() => _spared = true;

        public bool IsDustFinished => _dusting && _dustLeft <= 0f;

        // holds him still without changing how he looks, for his last words
        public void Freeze()
        {
            _frozen   = true;
            _hurtLeft = 0f; // otherwise he'd keep juddering while he talks
        }

        public void Update(float dt)
        {
            // the crumble still has to tick even though he's stopped moving
            if (_dusting)
            {
                if (_dustLeft > 0f) _dustLeft -= dt;
                return;
            }

            if (_spared || _frozen) return; // held still

            _time += dt;
            if (_hurtLeft > 0f) _hurtLeft -= dt;
        }

        // draws one part as a stack of horizontal slices. a slice's progress
        // depends on how far down the body it is, so the head comes apart while
        // the legs are still solid. each slice slides sideways, drifts up and
        // fades on its own
        private void DrawDissolved(SpriteBatch spriteBatch, Rectangle src, Vector2 pos,
            float s, float dustT, float charTop, float charHeight)
        {
            for (int y = 0; y < src.Height; y += BandHeight)
            {
                int h = Math.Min(BandHeight, src.Height - y);
                var bandSrc = new Rectangle(src.X, src.Y + y, src.Width, h);

                float bandY = pos.Y + y * s;
                float down  = charHeight > 0f
                    ? MathHelper.Clamp((bandY - charTop) / charHeight, 0f, 1f)
                    : 0f;

                float p = MathHelper.Clamp((dustT - down * TopDownDelay) / (1f - TopDownDelay), 0f, 1f);

                float drift = Hash(src.X + src.Y + y) * DustSpread * p * s;
                float rise  = -DustRise * p * s;

                spriteBatch.Draw(
                    _sheet.Texture,
                    new Rectangle((int)(pos.X + drift), (int)(bandY + rise),
                                  (int)(src.Width * s), (int)(h * s)),
                    bandSrc,
                    Color.White * (1f - p));
            }
        }

        // stable per slice so they spread apart steadily instead of flickering
        private static float Hash(int n)
        {
            n = (n << 13) ^ n;
            int m = (n * (n * n * 15731 + 789221) + 1376312589) & 0x7fffffff;
            return 1f - m / 1073741824f;
        }

        // scale squashes/stretches the whole assembled character as one group,
        // around a pivot planted at his feet — so a squash sinks him into the
        // floor rather than shrinking him toward his middle. null means 1:1,
        // which is every caller that isn't animating a leap
        public void Draw(SpriteBatch spriteBatch, Vector2 anchor, Vector2? scale = null)
        {
            if (!IsReady) return;

            Vector2 groupScale = scale ?? Vector2.One;

            // bottom-centre of the assembled character. Anchor is the art's own
            // nudge off the passed-in position, so it belongs in here too
            Vector2 pivot = anchor + _data.Anchor + new Vector2(Size.X / 2f, Size.Y);

            // the whole character jitters while hurt, dies down as it wears off
            // sideways only, and it's an oscillation rather than random jitter so
            // it reads as him rocking rather than vibrating. dies down as it ends
            Vector2 shake = Vector2.Zero;
            if (IsHurt && _hurtSpan > 0f)
            {
                float mag = _shakeStrength * (_hurtLeft / _hurtSpan);
                shake = new Vector2(MathF.Sin(_hurtLeft * ShakeHz * MathHelper.TwoPi) * mag, 0f);
            }

            float s = _data.Scale;

            float dustT = _dusting ? 1f - MathHelper.Clamp(_dustLeft / DustSeconds, 0f, 1f) : 0f;
            float alpha = _spared ? SparedAlpha : 1f;

            // where the whole character starts and how tall he is, so the slices
            // know how far down the body they are
            float charTop    = anchor.Y + _data.Anchor.Y;
            float charHeight = Size.Y;

            foreach (TeacherPartData part in _data.Parts)
            {
                // Speed is cycles per second, Phase is 0..1 of a cycle
                float wave = MathF.Sin((_time * part.Speed + part.Phase) * MathHelper.TwoPi);

                // offsets and bob scale with the art so the parts stay together
                Vector2 pos = anchor + _data.Anchor + shake
                            + (part.Offset + new Vector2(wave * part.BobX, wave * part.BobY)) * s;

                Rectangle src = IsHurt && part.SrcHurt.HasValue ? part.SrcHurt.Value : part.Src;

                if (_dusting)
                {
                    DrawDissolved(spriteBatch, src, pos, s, dustT, charTop, charHeight);
                    continue;
                }

                // the part keeps its place in the group: its corner moves with
                // the scale and its extent grows by the same factor, so the
                // whole body deforms together instead of parts drifting apart
                Vector2 drawPos = pivot + (pos - pivot) * groupScale;

                spriteBatch.Draw(
                    _sheet.Texture,
                    new Rectangle((int)drawPos.X, (int)drawPos.Y,
                        (int)(src.Width  * s * groupScale.X),
                        (int)(src.Height * s * groupScale.Y)),
                    src,
                    Color.White * alpha);
            }
        }
    }
}
