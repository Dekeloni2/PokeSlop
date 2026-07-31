using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using FinalProject.Core.Graphics;
using FinalProject.Core.Input;
using FinalProject.Core.Text;
using FinalProject.Core.Audio;

namespace FinalProject.UI
{
    // the white bubble the teacher talks through. black text, and the tail is on
    // the left so it sits to the right of whoever is talking.
    // a '|' in the line splits it into separate bubbles, same as the overworld
    // dialogue box. the line is prepared at the start of the turn but only
    // Begin()s when it should show, then it holds the turn until the player has
    // pressed Z through every page
    public class SpeechBubble
    {
        // insets for the text, the left one clears the tail and the rounded corner
        private const int PadLeft  = 42;
        private const int PadTop   = 20;
        private const int PadRight = 18;
        private const float TextScale = 1f;
        private const char PageBreak  = '|';

        private readonly Typewriter _typer = new();
        private readonly List<string> _pages = new();

        // how long a finished page sits before auto advancing, see UpdateText
        private const float AutoPageHold = 1.4f;

        private int   _page;
        private bool  _active;
        private float _autoHold;

        public bool HasText  => _pages.Count > 0;
        public bool IsActive => _active;
        public string BeepSound { get; set; }

        // splits on '|', wraps each page, and holds them without showing anything
        public void Prepare(string text, SpriteFont font)
        {
            _active = false;
            _page   = 0;
            _pages.Clear();

            if (string.IsNullOrWhiteSpace(text)) return;

            Spritesheet sheet = SpriteManager.GetSprite("textBubble");
            int maxWidth = (sheet?.Texture.Width ?? 233) - PadLeft - PadRight;

            foreach (string segment in text.Split(PageBreak))
            {
                string trimmed = segment.Trim();
                if (trimmed.Length == 0) continue; // ignore stray breaks

                _pages.Add(string.Join("\n", TextWrap.ToLines(font, trimmed, maxWidth, TextScale)));
            }
        }

        // starts typing the first page
        public void Begin()
        {
            if (!HasText) return;

            _page     = 0;
            _autoHold = 0f;
            _typer.BeepSound = BeepSound ?? SoundManager.TextBeepName;
            _typer.SetText(_pages[0]);
            _active = true;
        }

        // starts a line typing out without any of the Z handling, for lines that
        // aren't a conversation (the data type lesson). drive it with UpdateText
        public void ShowTyping(string text, SpriteFont font)
        {
            Prepare(text, font);
            Begin();
        }

        // advances the typing only. Update() is the version that also reads Z
        // and closes the bubble, which a non conversation line shouldn't do.
        //
        // pages still have to turn though, and during an attack the player is
        // busy dodging and can't press Z for them — so once a page has finished
        // typing it holds for a beat and then moves on by itself. without this
        // everything after the first '|' simply never appears, which quietly
        // swallowed most of the training dummy's instructions
        public void UpdateText(float dt)
        {
            if (!_active) return;

            _typer.Update(dt);

            if (!_typer.IsFullyShown) return;
            if (_page >= _pages.Count - 1) return; // last page stays up

            // the hold only starts once the page is fully typed, so a long page
            // gets its full reading time rather than being cut off early
            _autoHold += dt;
            if (_autoHold < AutoPageHold) return;

            _autoHold = 0f;
            _page++;
            _typer.SetText(_pages[_page]);
        }

        public void Clear()
        {
            _pages.Clear();
            _active = false;
        }

        // Z finishes the typing, then moves to the next page, and closes on the last
        public void Update(GameTime gameTime, InputManager input)
        {
            if (!_active) return;

            _typer.Update((float)gameTime.ElapsedGameTime.TotalSeconds);

            if (!input.IsKeyPressed(Keys.Z)) return;

            if (!_typer.IsFullyShown) { _typer.SkipToEnd(); return; }

            if (_page < _pages.Count - 1)
            {
                _page++;
                _typer.SetText(_pages[_page]);
                return;
            }

            _active = false;
        }

        public void Draw(SpriteBatch spriteBatch, SpriteFont font, Vector2 position)
        {
            if (!_active) return;

            Spritesheet sheet = SpriteManager.GetSprite("textBubble");
            if (sheet == null) return;

            spriteBatch.Draw(sheet.Texture, position, Color.White);

            spriteBatch.DrawString(font, _typer.VisibleText,
                position + new Vector2(PadLeft, PadTop), Color.Black,
                0f, Vector2.Zero, TextScale, SpriteEffects.None, 0f);
        }
    }
}
