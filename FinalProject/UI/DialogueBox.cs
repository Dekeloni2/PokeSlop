// UI/DialogueBox.cs
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using FinalProject.Core;
using FinalProject.Core.Input;
using FinalProject.Core.Text;

namespace FinalProject.UI
{
    public class DialogueBox
    {
        private const float CharsPerSecond  = 40f;
        private const int   MaxLinesPerPage = 3;
        private const int   PaddingX        = 12;
        private const int   PaddingY        = 10;
        private const int   BorderThickness = 3;

        private static readonly Keys[] AdvanceKeys = { Keys.Z, Keys.Enter, Keys.Space };

        private readonly Texture2D  _pixel;
        private readonly SpriteFont _font;
        private readonly Rectangle  _boxRect;

        private List<string> _pages = new();
        private int   _pageIndex;
        private float _charTimer;
        private int   _visibleChars;
        private bool  _pageFullyShown;
        private float _indicatorBlink;

        public bool IsActive { get; private set; }

        public DialogueBox(Texture2D pixelTexture, SpriteFont font)
        {
            _pixel = pixelTexture;
            _font  = font;

            int width  = GameSettings.WindowWidth - 32;
            int height = 84;
            int x      = 16;
            int y      = GameSettings.WindowHeight - height - 16;
            _boxRect = new Rectangle(x, y, width, height);
        }

        // Word-wraps and paginates the given text, then opens the box on page 1.
        public void Open(string text)
        {
            _pages          = Paginate(text ?? string.Empty);
            _pageIndex      = 0;
            _visibleChars   = 0;
            _charTimer      = 0f;
            _indicatorBlink = 0f;
            _pageFullyShown = _pages[0].Length == 0;
            IsActive        = true;
        }

        public void Close()
        {
            IsActive = false;
            _pages.Clear();
        }

        // Advances the typewriter effect and handles the advance/skip button.
        // Call once per frame while IsActive; safe to call otherwise (no-op).
        public void Update(GameTime gameTime, InputManager input)
        {
            if (!IsActive) return;

            _indicatorBlink += (float)gameTime.ElapsedGameTime.TotalSeconds;

            string current = _pages[_pageIndex];

            if (!_pageFullyShown)
            {
                _charTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
                _visibleChars = Math.Min((int)(_charTimer * CharsPerSecond), current.Length);
                if (_visibleChars >= current.Length)
                    _pageFullyShown = true;
            }

            if (!IsAdvancePressed(input)) return;

            if (!_pageFullyShown)
            {
                // First press on a page instantly reveals the rest of it,
                _visibleChars   = current.Length;
                _pageFullyShown = true;
                return;
            }

            if (_pageIndex < _pages.Count - 1)
            {
                _pageIndex++;
                _visibleChars   = 0;
                _charTimer      = 0f;
                _pageFullyShown = false;
            }
            else
            {
                Close();
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (!IsActive) return;

            spriteBatch.Draw(_pixel, _boxRect, Color.Black);
            DrawBorder(spriteBatch);

            string shown = _pages[_pageIndex].Substring(0, _visibleChars);
            var textPos  = new Vector2(_boxRect.X + PaddingX, _boxRect.Y + PaddingY);
            spriteBatch.DrawString(_font, shown, textPos, Color.White);

            // Blink a "press to continue" arrow once the page has fully typed out.
            if (_pageFullyShown && (int)(_indicatorBlink * 2f) % 2 == 0)
            {
                const string indicator = ">";
                Vector2 size = _font.MeasureString(indicator);
                var pos = new Vector2(
                    _boxRect.Right  - PaddingX - size.X,
                    _boxRect.Bottom - PaddingY - size.Y);
                spriteBatch.DrawString(_font, indicator, pos, Color.White);
            }
        }
        

        private static bool IsAdvancePressed(InputManager input)
        {
            foreach (Keys key in AdvanceKeys)
                if (input.IsKeyPressed(key))
                    return true;
            return false;
        }

        private void DrawBorder(SpriteBatch spriteBatch)
        {
            Rectangle boxRect = _boxRect;
            int thickness = BorderThickness;

            spriteBatch.Draw(_pixel, new Rectangle(boxRect.X, boxRect.Y, boxRect.Width, thickness), Color.White);            // top
            spriteBatch.Draw(_pixel, new Rectangle(boxRect.X, boxRect.Bottom - thickness, boxRect.Width, thickness), Color.White);    // bottom
            spriteBatch.Draw(_pixel, new Rectangle(boxRect.X, boxRect.Y, thickness, boxRect.Height), Color.White);            // left
            spriteBatch.Draw(_pixel, new Rectangle(boxRect.Right - thickness, boxRect.Y, thickness, boxRect.Height), Color.White);    // right
        }

        // Word-wraps text to fit the box width, then groups the resulting lines
        // into pages of MaxLinesPerPage lines (joined with '\n' — SpriteFont
        // draws/measures embedded newlines natively).
        private List<string> Paginate(string text)
        {
            int maxWidth = _boxRect.Width - PaddingX * 2;
            List<string> lines = TextWrap.ToLines(_font, text, maxWidth);

            var pages = new List<string>();
            for (int i = 0; i < lines.Count; i += MaxLinesPerPage)
            {
                int count = Math.Min(MaxLinesPerPage, lines.Count - i);
                pages.Add(string.Join("\n", lines.GetRange(i, count)));
            }

            if (pages.Count == 0)
                pages.Add(string.Empty);

            return pages;
        }
    }
}
