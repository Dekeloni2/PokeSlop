using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
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
            Spritesheet buttons = SpriteManager.GetSprite("battleButtons");
            Rectangle cell = buttons[0, 0];

            int totalWidth = cell.Width * 4 + ButtonGap * 3;
            int startX = (GameSettings.WindowWidth - totalWidth) / 2;
            int y = GameSettings.WindowHeight - cell.Height - 16;

            for (int i = 0; i < 4; i++)
                _buttonBounds[i] = new Rectangle(startX + i * (cell.Width + ButtonGap), y, cell.Width, cell.Height);
        }

        // left/right moves the cursor, returns true with the choice on Z
        public bool Update(InputManager input, out PlayerMoveList choice)
        {
            if (input.IsKeyPressed(Keys.Left))  _selectedIndex = (_selectedIndex + 3) % 4;
            if (input.IsKeyPressed(Keys.Right)) _selectedIndex = (_selectedIndex + 1) % 4;

            choice = (PlayerMoveList)_selectedIndex;
            return input.IsKeyPressed(Keys.Z) || input.IsKeyPressed(Keys.Enter);
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
