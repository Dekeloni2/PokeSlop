using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using FinalProject.Core.Audio;
using FinalProject.Core.Graphics;
using FinalProject.Core.Input;
using FinalProject.Core.Text;

namespace FinalProject.Battle
{
    // The menu pages shown inside the battle box (target lists, ACT options,
    // result text). Pages stack on top of each other, X goes back one page.
    //
    // Option lists lay out in two columns of three, filled top-to-bottom down
    // the left column first, the way Undertale does it. More than six entries
    // splits into numbered pages: right from the last slot goes forward, left
    // from the first slot comes back.
    public class ActionMenu
    {
        private const float TextScale = 2f;
        private const int   RightPadding = 16; // keeps wrapped text off the right border
        private const int   OptionGap    = 12; // extra vertical gap between distinct options
        private const int   TopPadding   = 16;

        private const int Columns  = 2;
        private const int Rows     = 3;
        private const int PerPage  = Columns * Rows;

        // each page also remembers whether X can cancel out of it, and whether
        // its text types out (result messages) or shows instantly (option lists)
        private readonly Stack<(List<MenuOption> Options, bool AllowCancel, bool Typewriter)> _pages = new();
        private readonly Typewriter _typewriter = new();
        private int _cursor;

        // set on Push, cleared on the first Draw once the box/font are known:
        // the typewriter text gets its line breaks baked in up front so the
        // wrap points can't shift while the text is still typing out
        private bool _typewriterNeedsLayout;

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
            {
                _typewriter.SetText(page[0].Text);
                _typewriterNeedsLayout = true;
            }
        }

        // returns false once the player backs out of the last page
        public bool Update(GameTime gameTime, InputManager input)
        {
            (List<MenuOption> page, bool allowCancel, bool typewriter) = _pages.Peek();

            if (typewriter)
                _typewriter.Update((float)gameTime.ElapsedGameTime.TotalSeconds);

            if (!typewriter)
            {
                int moved = MoveCursor(input, page.Count);
                if (moved != _cursor)
                {
                    _cursor = moved;
                    SoundManager.Play(SoundManager.MenuMove);
                }
            }

            if (allowCancel && (input.IsKeyPressed(Keys.X) || input.IsKeyPressed(Keys.RightShift)))
            {
                _pages.Pop();
                _cursor = 0;
                return _pages.Count > 0;
            }

            if (input.IsKeyPressed(Keys.Z) || input.IsKeyPressed(Keys.Enter))
            {
                // first Z finishes the typing, second Z actually confirms
                if (typewriter && !_typewriter.IsFullyShown)
                {
                    _typewriter.SkipToEnd();
                }
                else
                {
                    SoundManager.Play(SoundManager.MenuSelect);
                    page[_cursor].Activate();
                }
            }

            return true;
        }

        // grid movement. up/down walk a column and wrap inside it, left/right
        // swap columns, and the outer edges are where paging happens
        private int MoveCursor(InputManager input, int count)
        {
            int pageStart = PageStart(_cursor);
            int onPage    = Math.Min(PerPage, count - pageStart);
            int local     = _cursor - pageStart;
            int column    = local / Rows;
            int row       = local % Rows;

            // a short last page can leave the right column with fewer cells
            int columnStart = column * Rows;
            int inColumn    = Math.Min(Rows, onPage - columnStart);

            if (input.IsKeyPressed(Keys.Down) && inColumn > 0)
                return pageStart + columnStart + (row + 1) % inColumn;

            if (input.IsKeyPressed(Keys.Up) && inColumn > 0)
                return pageStart + columnStart + (row - 1 + inColumn) % inColumn;

            if (input.IsKeyPressed(Keys.Right))
            {
                if (column == 0)
                {
                    // same row of the right column, or its last entry if that
                    // row doesn't exist on a short page
                    int target = pageStart + Rows + row;
                    if (target < pageStart + onPage) return target;
                    if (onPage > Rows) return pageStart + onPage - 1;
                }
                else if (local == PerPage - 1 && pageStart + PerPage < count)
                {
                    return pageStart + PerPage; // off the last slot, next page
                }
            }

            if (input.IsKeyPressed(Keys.Left))
            {
                if (column == 1) return pageStart + row;
                if (pageStart > 0) return pageStart - 1; // back to the previous page's last slot
            }

            return _cursor;
        }

        private static int PageStart(int cursor) => cursor / PerPage * PerPage;

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
            float lineSpacingPx = font.LineSpacing * TextScale;

            if (typewriter)
            {
                DrawMessage(spriteBatch, font, box, page, textIndent);
                return;
            }

            int pageStart = PageStart(_cursor);
            int onPage    = Math.Min(PerPage, page.Count - pageStart);
            float cellWidth    = (box.Width - RightPadding) / (float)Columns;
            float maxTextWidth = cellWidth - textIndent - RightPadding;

            for (int local = 0; local < onPage; local++)
            {
                int index  = pageStart + local;
                int column = local / Rows;
                int row    = local % Rows;

                float cellX = box.X + column * cellWidth;
                float y     = box.Y + TopPadding + row * (lineSpacingPx + OptionGap);

                string text = string.Join("\n",
                    TextWrap.ToLines(font, "* " + page[index].Text, maxTextWidth, TextScale));

                spriteBatch.DrawString(font, text, new Vector2(cellX + textIndent, y),
                    page[index].Color, 0f, Vector2.Zero, TextScale, SpriteEffects.None, 0f);

                if (index == _cursor)
                {
                    var soulDest = new Rectangle(
                        (int)(cellX + SoulMarginLeft),
                        (int)(y + lineSpacingPx / 2f - soulSrc.Height / 2f),
                        soulSrc.Width, soulSrc.Height);
                    spriteBatch.Draw(soul.Texture, soulDest, soulSrc, Color.White);
                }
            }

            DrawPageNumber(spriteBatch, font, box, page.Count, pageStart);
        }

        // only worth showing once the list actually spills past one page
        private void DrawPageNumber(SpriteBatch spriteBatch, SpriteFont font, Rectangle box,
                                    int count, int pageStart)
        {
            if (count <= PerPage) return;

            int total = (count + PerPage - 1) / PerPage;
            string label = $"PAGE {pageStart / PerPage + 1}/{total}";

            Vector2 size = font.MeasureString(label) * TextScale;
            var position = new Vector2(
                box.Right - RightPadding - size.X,
                box.Bottom - TopPadding - size.Y);

            spriteBatch.DrawString(font, label, position, Color.White,
                0f, Vector2.Zero, TextScale, SpriteEffects.None, 0f);
        }

        // result text keeps the old full width single column layout, it's one
        // block of prose rather than something you pick from
        private void DrawMessage(SpriteBatch spriteBatch, SpriteFont font, Rectangle box,
                                 List<MenuOption> page, float textIndent)
        {
            float maxTextWidth = box.Width - textIndent - RightPadding;

            // bake the wrap into the typewriter text once, so revealing it a
            // character at a time never moves a letter that's already visible
            if (_typewriterNeedsLayout)
            {
                _typewriter.SetText(string.Join("\n",
                    TextWrap.ToLines(font, "* " + page[0].Text, maxTextWidth, TextScale)));
                _typewriterNeedsLayout = false;
            }

            spriteBatch.DrawString(font, _typewriter.VisibleText,
                new Vector2(box.X + textIndent, box.Y + TopPadding), page[0].Color,
                0f, Vector2.Zero, TextScale, SpriteEffects.None, 0f);
        }
    }
}
