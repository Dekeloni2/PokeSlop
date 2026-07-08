using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using FinalProject.Core;

namespace FinalProject.Battle
{
    // The menu pages shown inside the battle box (target lists, ACT options,
    // result text). Pages stack on top of each other, X goes back one page.
    public class ActionMenu
    {
        private const float TextScale = 2f;
        private const int   RightPadding = 16; // keeps wrapped text off the right border
        private const int   OptionGap    = 12; // extra vertical gap between distinct options

        // each page also remembers whether X can cancel out of it, and whether
        // its text types out (result messages) or shows instantly (option lists)
        private readonly Stack<(List<MenuOption> Options, bool AllowCancel, bool Typewriter)> _pages = new();
        private readonly Typewriter _typewriter = new();
        private int _cursor;

        public void Open(List<MenuOption> rootPage, bool allowCancel = true, bool typewriter = false)
        {
            _pages.Clear();
            Push(rootPage, allowCancel, typewriter);
        }

        public void Push(List<MenuOption> page, bool allowCancel = true, bool typewriter = false)
        {
            _pages.Push((page, allowCancel, typewriter));
            _cursor = 0;

            if (typewriter)
                _typewriter.SetText(page[0].Text);
        }

        // returns false once the player backs out of the last page
        public bool Update(GameTime gameTime, InputManager input)
        {
            (List<MenuOption> page, bool allowCancel, bool typewriter) = _pages.Peek();

            if (typewriter)
                _typewriter.Update((float)gameTime.ElapsedGameTime.TotalSeconds);

            if (input.IsKeyPressed(Keys.Down)) _cursor = (_cursor + 1) % page.Count;
            if (input.IsKeyPressed(Keys.Up))   _cursor = (_cursor - 1 + page.Count) % page.Count;

            if (allowCancel && input.IsKeyPressed(Keys.X))
            {
                _pages.Pop();
                _cursor = 0;
                return _pages.Count > 0;
            }

            if (input.IsKeyPressed(Keys.Z))
            {
                // first Z finishes the typing, second Z actually confirms
                if (typewriter && !_typewriter.IsFullyShown)
                    _typewriter.SkipToEnd();
                else
                    page[_cursor].Activate();
            }

            return true;
        }

        // left margin reserved for the soul cursor. Every line gets the same
        // indent so text doesn't shift when the cursor moves
        private const int SoulMarginLeft = 16;
        private const int SoulTextGap    = 10;

        public void Draw(SpriteBatch spriteBatch, SpriteFont font, Rectangle box)
        {
            (List<MenuOption> page, _, bool typewriter) = _pages.Peek();
            Spritesheet soul = SpriteManager.GetSprite("soul");
            Rectangle soulSrc = soul[0, 0];
            float textIndent  = SoulMarginLeft + soulSrc.Width + SoulTextGap;
            float maxTextWidth = box.Width - textIndent - RightPadding;
            float lineSpacingPx = font.LineSpacing * TextScale;

            float y = box.Y + 16;
            for (int i = 0; i < page.Count; i++)
            {
                Color color = Color.White;
                float optionStartY = y;

                string optionText = (typewriter && i == 0) ? _typewriter.VisibleText : page[i].Text;

                // wrap long text so it stays inside the box
                List<string> lines = TextWrap.ToLines(font, "* " + optionText, maxTextWidth, TextScale);
                foreach (string line in lines)
                {
                    spriteBatch.DrawString(font, line, new Vector2(box.X + textIndent, y), color,
                        0f, Vector2.Zero, TextScale, SpriteEffects.None, 0f);
                    y += lineSpacingPx;
                }

                // no soul on message pages, only on lists you actually pick from
                if (i == _cursor && !typewriter)
                {
                    var soulDest = new Rectangle(
                        box.X + SoulMarginLeft,
                        (int)(optionStartY + lineSpacingPx / 2f - soulSrc.Height / 2f),
                        soulSrc.Width, soulSrc.Height);
                    spriteBatch.Draw(soul.Texture, soulDest, soulSrc, Color.White);
                }

                y += OptionGap;
            }
        }
    }
}
