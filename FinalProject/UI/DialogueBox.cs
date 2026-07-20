// UI/DialogueBox.cs
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using FinalProject.Core;
using FinalProject.Core.Audio;
using FinalProject.Core.Input;
using FinalProject.Core.Text;

namespace FinalProject.UI
{
    public class DialogueBox
    {
        private const float CharsPerSecond  = 40f;
        private const char  PageBreak       = '|';  // in Text, forces a new page
        private const int   PaddingX        = 16;  // match the battle box
        private const int   PaddingY        = 16;
        private const int   BorderThickness = 2;   // battle box uses a 2px border
        private const float TextScale       = 2f;  // = BattleState.NarrationTextScale

        // Match the battle box (BattleState.WideBoxRect) size exactly.
        private const int   BoxWidth        = 550;
        private const int   BoxHeight       = 118;

        private static readonly Keys[] AdvanceKeys = { Keys.Z, Keys.Enter, Keys.Space };

        private readonly Texture2D  _pixel;
        private readonly SpriteFont _font;
        private readonly Rectangle  _boxRect;
        private readonly int        _maxLinesPerPage;

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

            // Same size as the battle box, centered horizontally and anchored
            // near the bottom of the screen.
            int x = (GameSettings.WindowWidth - BoxWidth) / 2;
            int y = GameSettings.WindowHeight - BoxHeight - 16;
            _boxRect = new Rectangle(x, y, BoxWidth, BoxHeight);

            // fit as many lines as the fixed height allows at the battle text scale
            int lineHeight   = (int)(_font.LineSpacing * TextScale);
            _maxLinesPerPage = Math.Max(1, (BoxHeight - PaddingY * 2) / lineHeight);
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
                int revealed = Math.Min((int)(_charTimer * CharsPerSecond), current.Length);

                // blip once per frame for each newly revealed non-space character
                if (revealed > _visibleChars)
                {
                    SoundManager.PlayTextBeep(current, _visibleChars, revealed);
                    _visibleChars = revealed;
                }

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
            spriteBatch.DrawString(_font, shown, textPos, Color.White,
                0f, Vector2.Zero, TextScale, SpriteEffects.None, 0f);

            // Blink a "press to continue" arrow once the page has fully typed out.
            if (_pageFullyShown && (int)(_indicatorBlink * 2f) % 2 == 0)
            {
                const string indicator = ">";
                Vector2 size = _font.MeasureString(indicator) * TextScale;
                var pos = new Vector2(
                    _boxRect.Right  - PaddingX - size.X,
                    _boxRect.Bottom - PaddingY - size.Y);
                spriteBatch.DrawString(_font, indicator, pos, Color.White,
                    0f, Vector2.Zero, TextScale, SpriteEffects.None, 0f);
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

        // Splits the text into pages. A '|' in the source is an explicit page
        // break authored on the interactable — each segment starts a fresh page.
        // Within a segment, text is word-wrapped to the box width and, if it's
        // still longer than fits, spills onto further pages automatically. '\n'
        // forces a line break inside a page. Pages advance with the Z key.
        private List<string> Paginate(string text)
        {
            int maxWidth = _boxRect.Width - PaddingX * 2;
            var pages = new List<string>();

            foreach (string segment in text.Split(PageBreak))
            {
                string trimmed = segment.Trim();
                if (trimmed.Length == 0) continue; // ignore empty/stray breaks

                List<string> lines = TextWrap.ToLines(_font, trimmed, maxWidth, TextScale);
                for (int i = 0; i < lines.Count; i += _maxLinesPerPage)
                {
                    int count = Math.Min(_maxLinesPerPage, lines.Count - i);
                    pages.Add(string.Join("\n", lines.GetRange(i, count)));
                }
            }

            if (pages.Count == 0)
                pages.Add(string.Empty);

            return pages;
        }
    }
}
