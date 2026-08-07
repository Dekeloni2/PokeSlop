using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using FinalProject.Core;
using FinalProject.Core.Audio;
using FinalProject.Core.Input;

namespace FinalProject.UI
{
    // a small number-pad: one column per digit, each dialed up or down by
    // hand rather than typed — Undertale's keypad puzzles are the reference.
    // Left/Right move which digit is selected, Up/Down spin it 0-9 wrapping,
    // Z confirms whatever's showing. There's no "correct" answer wired in
    // here — the caller (OverworldState's Action "keypad") decides what a
    // confirmed code means, this just collects the four digits.
    public class KeypadEntry
    {
        private const int DigitCount = 4;

        private const int   BoxWidth        = 260;
        private const int   BoxHeight       = 110;
        private const int   BorderThickness = 3;
        private const float DigitScale      = 3f;
        private const float ArrowScale      = 2f;
        private const int   DigitGap        = 26; // gap between digit columns
        private const int   ArrowGap        = 6;  // gap between a digit and its arrow

        // the reference gif's arrows are plain carets, not custom art — same
        // trick DialogueBox uses for its "press to continue" indicator
        private const string UpArrow   = "^";
        private const string DownArrow = "v";

        private readonly Texture2D  _pixel;
        private readonly SpriteFont _font;
        private readonly Rectangle  _boxRect;

        private readonly int[] _digits = new int[DigitCount];
        private int _selected;

        public bool IsActive { get; private set; }

        public KeypadEntry(Texture2D pixel, SpriteFont font)
        {
            _pixel = pixel;
            _font  = font;

            _boxRect = new Rectangle(
                (GameSettings.WindowWidth  - BoxWidth)  / 2,
                (GameSettings.WindowHeight - BoxHeight) / 2,
                BoxWidth, BoxHeight);
        }

        // every digit starts back at 0 — nothing carries over between tries
        public void Open()
        {
            System.Array.Clear(_digits, 0, _digits.Length);
            _selected = 0;
            IsActive  = true;
        }

        // returns true the frame Z confirms a code
        public bool Update(InputManager input)
        {
            if (!IsActive) return false;

            if (input.IsKeyPressed(Keys.Left))
            {
                _selected = (_selected - 1 + DigitCount) % DigitCount;
                SoundManager.Play(SoundManager.MenuMove);
            }
            else if (input.IsKeyPressed(Keys.Right))
            {
                _selected = (_selected + 1) % DigitCount;
                SoundManager.Play(SoundManager.MenuMove);
            }

            if (input.IsKeyPressed(Keys.Up))
            {
                _digits[_selected] = (_digits[_selected] + 1) % 10;
                SoundManager.Play(SoundManager.MenuMove);
            }
            else if (input.IsKeyPressed(Keys.Down))
            {
                _digits[_selected] = (_digits[_selected] + 9) % 10;
                SoundManager.Play(SoundManager.MenuMove);
            }

            if (input.IsKeyPressed(Keys.Z) || input.IsKeyPressed(Keys.Enter))
            {
                SoundManager.Play(SoundManager.MenuSelect);
                IsActive = false;
                return true;
            }

            // backing out without submitting anything — same X/Escape every
            // other menu in the overworld uses
            if (input.IsKeyPressed(Keys.X) || input.IsKeyPressed(Keys.RightShift) || input.IsKeyPressed(Keys.Escape))
                IsActive = false;

            return false;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (!IsActive) return;

            spriteBatch.Draw(_pixel, _boxRect, Color.Black);
            DrawBorder(spriteBatch);

            Vector2 digitSize = _font.MeasureString("0") * DigitScale;
            float   totalWidth = digitSize.X * DigitCount + DigitGap * (DigitCount - 1);
            float   startX     = _boxRect.Center.X - totalWidth / 2f;
            float   midY       = _boxRect.Center.Y;

            for (int i = 0; i < DigitCount; i++)
            {
                float centerX = startX + i * (digitSize.X + DigitGap) + digitSize.X / 2f;

                spriteBatch.DrawString(_font, _digits[i].ToString(),
                    new Vector2(centerX - digitSize.X / 2f, midY - digitSize.Y / 2f),
                    Color.White, 0f, Vector2.Zero, DigitScale, SpriteEffects.None, 0f);

                // only the digit being dialed gets arrows — matches the
                // reference, which shows one pair floating over one digit
                if (i != _selected) continue;

                Vector2 upSize   = _font.MeasureString(UpArrow)   * ArrowScale;
                Vector2 downSize = _font.MeasureString(DownArrow) * ArrowScale;

                spriteBatch.DrawString(_font, UpArrow,
                    new Vector2(centerX - upSize.X / 2f, midY - digitSize.Y / 2f - ArrowGap - upSize.Y),
                    Color.Gray, 0f, Vector2.Zero, ArrowScale, SpriteEffects.None, 0f);

                spriteBatch.DrawString(_font, DownArrow,
                    new Vector2(centerX - downSize.X / 2f, midY + digitSize.Y / 2f + ArrowGap),
                    Color.Gray, 0f, Vector2.Zero, ArrowScale, SpriteEffects.None, 0f);
            }
        }

        private void DrawBorder(SpriteBatch spriteBatch)
        {
            Rectangle r = _boxRect;
            int t = BorderThickness;

            spriteBatch.Draw(_pixel, new Rectangle(r.X, r.Y, r.Width, t), Color.White);
            spriteBatch.Draw(_pixel, new Rectangle(r.X, r.Bottom - t, r.Width, t), Color.White);
            spriteBatch.Draw(_pixel, new Rectangle(r.X, r.Y, t, r.Height), Color.White);
            spriteBatch.Draw(_pixel, new Rectangle(r.Right - t, r.Y, t, r.Height), Color.White);
        }
    }
}
