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

        // The face sits in a fixed-width slot on the left. Fixed, because heads
        // differ a lot in size (david's is 52x64, yakir's 32x42) and the text's
        // left edge must not jump around as the speaker changes.
        private const int   PortraitSlot     = 72;
        private const int   PortraitGap      = 16; // clear space between face and text
        private const float MaxPortraitScale = 3f;

        private static readonly Keys[] AdvanceKeys = { Keys.Z, Keys.Enter, Keys.Space };

        private readonly Texture2D  _pixel;
        private readonly SpriteFont _font;
        private readonly Rectangle  _boxRect;
        private readonly int        _maxLinesPerPage;

        private Speaker _speaker;

        // Where the text column starts and how wide it is. Both shift right when
        // there's a face, and pagination reads them — so wrapping happens against
        // the narrowed column instead of laying lines under the portrait.
        private int TextLeft  => _boxRect.X + PaddingX
                               + (_speaker != null && _speaker.HasFace ? PortraitSlot + PortraitGap : 0);
        private int TextWidth => _boxRect.Right - PaddingX - TextLeft;

        // Whether Z can fast-forward the typewriter. Anything with a speaker
        // behind it is scripted dialogue and has to play out at its own pace;
        // signposts and prompts stay skippable.
        private bool CanSkipTyping => _speaker == null;

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
        public void Open(string text) => Open(text, null);

        // Same, but attributed to a speaker: their face on the left and their
        // own text blip. A null speaker is identical to the plain overload.
        public void Open(string text, Speaker speaker)
        {
            // set before paginating — the face is what decides the column width
            _speaker        = speaker;
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

                // blip once per frame for each newly revealed non-space character,
                // in the speaker's own voice — same key the battle speech bubble
                // uses, so a teacher sounds identical in and out of a fight
                if (revealed > _visibleChars)
                {
                    SoundManager.PlayTextBeep(
                        _speaker?.Voice ?? SoundManager.TextBeepName, current, _visibleChars, revealed);
                    _visibleChars = revealed;
                }

                if (_visibleChars >= current.Length)
                    _pageFullyShown = true;
            }

            if (!IsAdvancePressed(input)) return;

            if (!_pageFullyShown)
            {
                // Attributed dialogue can't be rushed — a press while it's still
                // typing does nothing at all. Same rule as the GAME OVER text:
                // a line someone is delivering shouldn't be mashable.
                if (!CanSkipTyping) return;

                // Plain overworld dialogue keeps the quick skip: the first press
                // on a page instantly reveals the rest of it.
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
            DrawPortrait(spriteBatch);

            string shown = _pages[_pageIndex].Substring(0, _visibleChars);
            var textPos  = new Vector2(TextLeft, _boxRect.Y + PaddingY);
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
        

        // Draws the face centred in its slot. The scale is picked so the head can
        // never spill past the slot into the text column, whatever size the art
        // is: whole-number steps while there's room to grow (pixel art smears at
        // fractional scales), and a plain fit-down if a head is ever too big.
        private void DrawPortrait(SpriteBatch spriteBatch)
        {
            if (_speaker == null || !_speaker.HasFace) return;

            Rectangle src = _speaker.FaceSource;
            if (src.Width <= 0 || src.Height <= 0) return;

            int slotHeight = _boxRect.Height - PaddingY * 2;

            float fit = Math.Min(PortraitSlot / (float)src.Width,
                                 slotHeight   / (float)src.Height);
            float scale = fit >= 1f ? MathF.Floor(Math.Min(fit, MaxPortraitScale)) : fit;

            int width  = Math.Max(1, (int)(src.Width  * scale));
            int height = Math.Max(1, (int)(src.Height * scale));

            var dest = new Rectangle(
                _boxRect.X + PaddingX + (PortraitSlot - width) / 2,
                _boxRect.Y + (_boxRect.Height - height) / 2,
                width, height);

            spriteBatch.Draw(_speaker.FaceTexture, dest, src, Color.White);
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

        // splits the text into pages. a '|' in the interactable's text forces a
        // new page. inside a segment the text wraps to the box width and spills
        // onto more pages if it's too long. '\n' is a line break inside a page.
        // pages advance with Z
        private List<string> Paginate(string text)
        {
            int maxWidth = TextWidth;
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
