using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using FinalProject.Core;
using FinalProject.Core.Input;
using FinalProject.Data;

namespace FinalProject.UI
{
    public class OverworldMenu
    {
        private enum MenuState
        {
            Main,
            Item,
            Stat,
            Cell
        }

        private readonly Texture2D _pixel;
        private readonly SpriteFont _font;

        public bool IsActive { get; private set; }

        private MenuState _state = MenuState.Main;
        private int _mainIndex = 0;
        private int _itemIndex = 0;

        private readonly string[] _mainOptions = { "ITEM", "STAT", "CELL" };

        public OverworldMenu(Texture2D pixel, SpriteFont font)
        {
            _pixel = pixel;
            _font = font;
        }

        public void Open()
        {
            IsActive = true;
            _state = MenuState.Main;
            _mainIndex = 0;
            _itemIndex = 0;
        }

        public void Close()
        {
            IsActive = false;
            _state = MenuState.Main;
        }

        public void Update(InputManager input, PlayerData playerData, Action<string> onUseItem = null)
        {
            if (!IsActive) return;

            switch (_state)
            {
                case MenuState.Main:
                    UpdateMain(input);
                    break;

                case MenuState.Item:
                    UpdateItem(input, playerData, onUseItem);
                    break;

                case MenuState.Stat:
                case MenuState.Cell:
                    // Any cancel / accept key returns to main menu
                    if (input.IsKeyPressed(Keys.X) || input.IsKeyPressed(Keys.C) ||
                        input.IsKeyPressed(Keys.Z) || input.IsKeyPressed(Keys.Enter) ||
                        input.IsKeyPressed(Keys.Escape))
                    {
                        _state = MenuState.Main;
                    }
                    break;
            }
        }

        private void UpdateMain(InputManager input)
        {
            // Close the menu with X, C or Esc
            if (input.IsKeyPressed(Keys.X) || input.IsKeyPressed(Keys.C) || input.IsKeyPressed(Keys.Escape))
            {
                Close();
                return;
            }
            
            if (input.IsKeyPressed(Keys.Up))
            {
                _mainIndex = (_mainIndex - 1 + _mainOptions.Length) % _mainOptions.Length;
            }
            else if (input.IsKeyPressed(Keys.Down))
            {
                _mainIndex = (_mainIndex + 1) % _mainOptions.Length;
            }

            // Proceed with Z or Enter
            if (input.IsKeyPressed(Keys.Z) || input.IsKeyPressed(Keys.Enter))
            {
                if (_mainIndex == 0) // ITEM
                {
                    _state = MenuState.Item;
                    _itemIndex = 0;
                }
                else if (_mainIndex == 1) // STAT
                {
                    _state = MenuState.Stat;
                }
                else if (_mainIndex == 2) // CELL
                {
                    _state = MenuState.Cell;
                }
            }
        }

        private void UpdateItem(InputManager input, PlayerData playerData, Action<string> onUseItem)
        {
            // Back out to main menu
            if (input.IsKeyPressed(Keys.X) || input.IsKeyPressed(Keys.C) || input.IsKeyPressed(Keys.Escape))
            {
                _state = MenuState.Main;
                return;
            }

            var inventory = playerData?.Inventory;
            if (inventory == null || inventory.Count == 0)
                return;

            if (input.IsKeyPressed(Keys.Up))
            {
                _itemIndex = (_itemIndex - 1 + inventory.Count) % inventory.Count;
            }
            else if (input.IsKeyPressed(Keys.Down))
            {
                _itemIndex = (_itemIndex + 1) % inventory.Count;
            }

            if (input.IsKeyPressed(Keys.Z) || input.IsKeyPressed(Keys.Enter))
            {
                ItemData item = inventory[_itemIndex];

                // Heal if item has HealAmount property
                if (item.HealAmount > 0)
                { 
                    playerData.Heal(item.HealAmount);
                }

                inventory.RemoveAt(_itemIndex);
                onUseItem?.Invoke($"* You used the {item.Name}.");

                if (inventory.Count == 0)
                {
                    _state = MenuState.Main;
                }
                else if (_itemIndex >= inventory.Count)
                {
                    _itemIndex = inventory.Count - 1;
                }
            }
        }

        public void Draw(SpriteBatch spriteBatch, PlayerData playerData)
        {
            if (!IsActive) return;

            // --- 1. Main Left Command Box ---
            Rectangle mainBox = new Rectangle(20, 40, 100, 110);
            DrawBorderedBox(spriteBatch, mainBox, Color.Black, Color.White, 3);

            for (int i = 0; i < _mainOptions.Length; i++)
            {
                Vector2 pos = new Vector2(mainBox.X + 28, mainBox.Y + 12 + (i * 30));
                
                if (_state == MenuState.Main && i == _mainIndex)
                {
                    DrawScaledText(spriteBatch, ">", new Vector2(pos.X - 16, pos.Y), Color.Red);
                }

                DrawScaledText(spriteBatch, _mainOptions[i], pos, Color.White);
            }

            // --- 2. Left Player Overview Box ---
            Rectangle playerBox = new Rectangle(20, 160, 100, 80);
            DrawBorderedBox(spriteBatch, playerBox, Color.Black, Color.White, 3);

            int hp = playerData?.CurrentHp ?? 20;
            int maxHp = playerData?.MaxHp ?? 20;
            int money = playerData?.Money ?? 0;

            DrawScaledText(spriteBatch, "CLOVER", new Vector2(playerBox.X + 10, playerBox.Y + 8), Color.White);
            DrawScaledText(spriteBatch, $"HP {hp}/{maxHp}", new Vector2(playerBox.X + 10, playerBox.Y + 30), Color.White);
            DrawScaledText(spriteBatch, $"G  {money}", new Vector2(playerBox.X + 10, playerBox.Y + 52), Color.White);

            // --- 3. Submenu Panel (Right Side) ---
            if (_state != MenuState.Main)
            {
                Rectangle subBox = new Rectangle(130, 40, 210, 200);
                DrawBorderedBox(spriteBatch, subBox, Color.Black, Color.White, 3);

                switch (_state)
                {
                    case MenuState.Item:
                        DrawItemSubmenu(spriteBatch, subBox, playerData);
                        break;

                    case MenuState.Stat:
                        DrawStatSubmenu(spriteBatch, subBox, playerData);
                        break;

                    case MenuState.Cell:
                        DrawCellSubmenu(spriteBatch, subBox);
                        break;
                }
            }
        }

        private void DrawItemSubmenu(SpriteBatch spriteBatch, Rectangle rect, PlayerData playerData)
        {
            var inventory = playerData?.Inventory;
            if (inventory == null || inventory.Count == 0)
            {
                DrawScaledText(spriteBatch, "USE ITEMS", new Vector2(rect.X + 15, rect.Y + 15), Color.Gray);
                DrawScaledText(spriteBatch, "(No items)", new Vector2(rect.X + 15, rect.Y + 45), Color.DarkGray);
                return;
            }

            for (int i = 0; i < inventory.Count; i++)
            {
                Vector2 pos = new Vector2(rect.X + 30, rect.Y + 15 + (i * 24));

                if (i == _itemIndex)
                {
                    DrawScaledText(spriteBatch, ">", new Vector2(pos.X - 16, pos.Y), Color.Red);
                }

                DrawScaledText(spriteBatch, inventory[i].Name, pos, Color.White);
            }
        }

        private void DrawStatSubmenu(SpriteBatch spriteBatch, Rectangle rect, PlayerData playerData)
        {
            int hp = playerData?.CurrentHp ?? 20;
            int maxHp = playerData?.MaxHp ?? 20;
            int atk = playerData?.Attack ?? 10;
            int money = playerData?.Money ?? 10;

            DrawScaledText(spriteBatch, "\"CLOVER\"", new Vector2(rect.X + 15, rect.Y + 15), Color.White);
            DrawScaledText(spriteBatch, $"AT  {atk}", new Vector2(rect.X + 15, rect.Y + 45), Color.White);
            DrawScaledText(spriteBatch, $"HP  {hp}/{maxHp}", new Vector2(rect.X + 15, rect.Y + 75), Color.White);
            DrawScaledText(spriteBatch, $"GOLD {money}", new Vector2(rect.X + 15, rect.Y + 105), Color.White);
        }

        private void DrawCellSubmenu(SpriteBatch spriteBatch, Rectangle rect)
        {
            DrawScaledText(spriteBatch, "CELL", new Vector2(rect.X + 15, rect.Y + 15), Color.White);
            DrawScaledText(spriteBatch, "* No response...", new Vector2(rect.X + 15, rect.Y + 50), Color.Gray);
        }

        private void DrawBorderedBox(SpriteBatch spriteBatch, Rectangle rect, Color bgColor, Color borderColor, int thickness)
        {
            spriteBatch.Draw(_pixel, rect, bgColor);

            spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), borderColor);
            spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), borderColor);
            spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), borderColor);
            spriteBatch.Draw(_pixel, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), borderColor);
        }
        
        private void DrawScaledText(SpriteBatch spriteBatch, string text, Vector2 position, Color color, float scale = 1.5f)
        {
            spriteBatch.DrawString(_font, text, position, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }
    }
}