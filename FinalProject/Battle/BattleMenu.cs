using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using FinalProject.Core.Audio;
using FinalProject.Core;
using FinalProject.Core.Graphics;
using FinalProject.Core.Input;
using FinalProject.States;

namespace FinalProject.Battle
{
    // The FIGHT/ACT/ITEM/MERCY button row at the bottom of the battle screen.
    public class BattleMenu
    {
        private const int ButtonGap = 20;

        // the atlas rows are ordered Act, Fight, Item, Mercy which doesn't
        // match PlayerMoveList's order, so remap
        private static readonly int[] AtlasRow = { 1, 0, 2, 3 };

        private readonly Rectangle[] _buttonBounds = new Rectangle[4];
        private int _selectedIndex;

        public BattleMenu()
        {
            Rectangle cell = SpriteManager.GetSprite("battleButtons")[0, 0];
            for (int i = 0; i < 4; i++)
                _buttonBounds[i] = ButtonBounds(i, cell);
        }

        // shared button layout — also used by the battle-intro transition so the
        // soul lands exactly on the FIGHT button (no pop when the battle starts)
        private static Rectangle ButtonBounds(int index, Rectangle cell)
        {
            int totalWidth = cell.Width * 4 + ButtonGap * 3;
            int startX = (GameSettings.WindowWidth - totalWidth) / 2;
            int y = GameSettings.WindowHeight - cell.Height - 16;
            return new Rectangle(startX + index * (cell.Width + ButtonGap), y, cell.Width, cell.Height);
        }

        // screen position the soul rests at on the FIGHT button (index 0)
        public static Vector2 FightSoulPosition()
        {
            Rectangle cell    = SpriteManager.GetSprite("battleButtons")[0, 0];
            Rectangle soulSrc = SpriteManager.GetSprite("soul")[0, 0];
            Rectangle b = ButtonBounds(0, cell);
            return new Vector2(b.X + 8 + soulSrc.Width / 2f, b.Y + b.Height / 2f);
        }

        // left/right moves the cursor, returns true with the choice on Z
        public bool Update(InputManager input, out PlayerMoveList choice)
        {
            if (input.IsKeyPressed(Keys.Left) || input.IsKeyPressed(Keys.Right))
                SoundManager.Play(SoundManager.MenuMove);

            if (input.IsKeyPressed(Keys.Left))  _selectedIndex = (_selectedIndex + 3) % 4;
            if (input.IsKeyPressed(Keys.Right)) _selectedIndex = (_selectedIndex + 1) % 4;

            choice = (PlayerMoveList)_selectedIndex;
            if (!input.IsKeyPressed(Keys.Z) && !input.IsKeyPressed(Keys.Enter)) return false;

            SoundManager.Play(SoundManager.MenuSelect);
            return true;
        }

        // showSoul is false once the player is inside a submenu - the soul
        // moves into the box at that point, but the button stays highlighted
        public void Draw(SpriteBatch spriteBatch, bool showSoul = true)
        {
            Spritesheet buttons = SpriteManager.GetSprite("battleButtons");

            for (int i = 0; i < _buttonBounds.Length; i++)
            {
                int col = i == _selectedIndex ? 1 : 0; // yellow if selected, else orange
                spriteBatch.Draw(buttons.Texture, _buttonBounds[i], buttons[col, AtlasRow[i]], Color.White);
            }

            if (!showSoul) return;

            // soul sits on the left side of the selected button
            Spritesheet soul = SpriteManager.GetSprite("soul");
            Rectangle sel = _buttonBounds[_selectedIndex];
            Rectangle soulSrc = soul[0, 0];
            var soulDest = new Rectangle(
                sel.X + 8, sel.Y + sel.Height / 2 - soulSrc.Height / 2,
                soulSrc.Width, soulSrc.Height);
            spriteBatch.Draw(soul.Texture, soulDest, soulSrc, Color.White);
        }
    }
}
