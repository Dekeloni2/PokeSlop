using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using FinalProject.Core;
using FinalProject.Core.Audio;
using FinalProject.Core.Graphics;
using FinalProject.Core.Input;

namespace FinalProject.UI
{
    // a grid of options with the soul as the cursor, like undertale's elevator.
    // options fill down the left column first, then the right one
    public class ChoiceBox
    {
        private const int   Columns   = 2;
        private const int   PaddingX  = 24;
        private const int   PaddingY  = 18;
        private const int   CursorGap = 14; // between the soul and the label
        private const int   BorderThickness = 2;
        private const float TextScale = 2f;
        private const int   BoxHeight = 130;

        private readonly Texture2D  _pixel;
        private readonly SpriteFont _font;
        private readonly Rectangle  _boxRect;

        private List<string> _options = new();
        private int _cursor;

        public bool IsActive { get; private set; }

        // set when the player confirms, -1 while nothing is chosen
        public int SelectedIndex { get; private set; } = -1;

        private int Rows => Math.Max(1, (int)Math.Ceiling(_options.Count / (float)Columns));

        public ChoiceBox(Texture2D pixelTexture, SpriteFont font)
        {
            _pixel = pixelTexture;
            _font  = font;

            int width = GameSettings.WindowWidth - 32;
            _boxRect = new Rectangle(
                16, GameSettings.WindowHeight - BoxHeight - 16, width, BoxHeight);
        }

        public void Open(List<string> options)
        {
            _options      = options;
            _cursor       = 0;
            SelectedIndex = -1;
            IsActive      = true;
        }

        public void Close() => IsActive = false;

        // returns true on the frame a choice is confirmed
        public bool Update(InputManager input)
        {
            if (!IsActive || _options.Count == 0) return false;

            int rows = Rows;
            int col  = _cursor / rows;
            int row  = _cursor % rows;

            bool moved = false;
            if (input.IsKeyPressed(Keys.Up))    { row = (row - 1 + rows) % rows;       moved = true; }
            if (input.IsKeyPressed(Keys.Down))  { row = (row + 1) % rows;              moved = true; }
            if (input.IsKeyPressed(Keys.Left))  { col = (col - 1 + Columns) % Columns; moved = true; }
            if (input.IsKeyPressed(Keys.Right)) { col = (col + 1) % Columns;           moved = true; }

            if (moved)
            {
                // the grid can have a gap at the end, don't land on it
                int target = col * rows + row;
                if (target < _options.Count) _cursor = target;
                SoundManager.Play(SoundManager.MenuMove);
            }

            if (!input.IsKeyPressed(Keys.Z) && !input.IsKeyPressed(Keys.Enter)) return false;

            SoundManager.Play(SoundManager.MenuSelect);
            SelectedIndex = _cursor;
            IsActive = false;
            return true;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (!IsActive) return;

            spriteBatch.Draw(_pixel, _boxRect, Color.Black);
            DrawBorder(spriteBatch);

            Spritesheet soul = SpriteManager.GetSprite("soul");
            Rectangle soulSrc = soul != null ? soul[0, 0] : Rectangle.Empty;

            int   rows       = Rows;
            float lineHeight = _font.LineSpacing * TextScale;
            int   colWidth   = (_boxRect.Width - PaddingX * 2) / Columns;

            for (int i = 0; i < _options.Count; i++)
            {
                int col = i / rows;
                int row = i % rows;

                float cursorX = _boxRect.X + PaddingX + col * colWidth;
                float y       = _boxRect.Y + PaddingY + row * lineHeight;

                spriteBatch.DrawString(_font, _options[i],
                    new Vector2(cursorX + soulSrc.Width + CursorGap, y), Color.White,
                    0f, Vector2.Zero, TextScale, SpriteEffects.None, 0f);

                if (i == _cursor && soul != null)
                    spriteBatch.Draw(soul.Texture,
                        new Rectangle((int)cursorX,
                                      (int)(y + lineHeight / 2f - soulSrc.Height / 2f),
                                      soulSrc.Width, soulSrc.Height),
                        soulSrc, Color.White);
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
