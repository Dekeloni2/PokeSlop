using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using FinalProject.Core;
using FinalProject.Core.Audio;
using FinalProject.Core.Graphics;
using FinalProject.Core.Input;
using FinalProject.Data;

namespace FinalProject.UI
{
    // The overworld pause menu (C). Laid out to match Undertale's: a name/stat
    // box and an ITEM/STAT/CELL/SETTINGS box stacked down the left, and a tall
    // panel on the right that only appears once you're inside a submenu.
    public class OverworldMenu
    {
        private enum MenuState
        {
            Main,
            Item,
            Stat,
            Cell,
            Settings
        }

        // ── Layout ───────────────────────────────────────────────────────────
        // The whole menu is one block, centred on screen. The item panel is
        // sized to its eight rows rather than stretched to the window edges.
        private const int BorderThickness = 5;

        private const int LeftWidth   = 190;
        private const int ColumnGap   = 20;
        private const int RightWidth  = 380;
        private const int RightHeight = 330;
        private const int NameHeight  = 108;
        private const int MenuHeight  = 180; // tall enough for a 4th row (SETTINGS)
        private const int RowGap      = 22; // between the two left boxes

        private const int BlockWidth = LeftWidth + ColumnGap + RightWidth;

        private static readonly int BlockX = (GameSettings.WindowWidth  - BlockWidth)  / 2;
        private static readonly int BlockY = (GameSettings.WindowHeight - RightHeight) / 2;

        private static readonly Rectangle NameBox  = new(BlockX, BlockY, LeftWidth, NameHeight);
        private static readonly Rectangle MenuBox  = new(BlockX, BlockY + NameHeight + RowGap,
            LeftWidth, MenuHeight);
        private static readonly Rectangle RightBox = new(BlockX + LeftWidth + ColumnGap, BlockY,
            RightWidth, RightHeight);

        // The pixel font is an 8px design, so whole-number scales stay crisp.
        // 1.5 is only used for the small stat rows, where the reference art has
        // text noticeably smaller than the headings.
        private const float HeadingScale = 2f;
        private const float StatScale    = 1.5f;

        // name box: heading, then HP/G as a label column and a value column
        // (no LV row — this game has no levels)
        private const int NameLabelX  = 12;
        private const int NameValueX  = 62;
        private const int NameHeadY   = 16;
        private const int NameStatY   = 50;
        private const int NameStatGap = 23;

        // ITEM/STAT/CELL — indented to leave room for the heart cursor
        private const int MainOptionX   = 60;
        private const int MainHeartX    = 30;
        private const int MainOptionY   = 26;
        private const int MainOptionGap = 38;

        // right panel item list
        private const int ItemTextX  = 34;
        private const int ItemHeartX = 13;
        private const int ItemFirstY = 22;
        private const int ItemGap    = 30;

        // USE / INFO / DROP sit as one centred group with a fixed gap between
        // them, rather than being spread across the full panel width.
        private const int ActionGap       = 40;
        private const int ActionBottomPad = 24;

        private static readonly Color HighlightColor = Color.Yellow;

        private readonly Texture2D  _pixel;
        private readonly SpriteFont _font;

        public bool IsActive { get; private set; }

        private MenuState _state = MenuState.Main;
        private int _mainIndex   = 0;
        private int _itemIndex   = 0;
        private int _actionIndex = 0; // 0 USE, 1 INFO, 2 DROP

        private readonly string[] _mainOptions   = { "ITEM", "STAT", "CELL", "SETTINGS" };
        private readonly string[] _actionOptions = { "USE", "INFO", "DROP" };

        // shared with MainMenuState's settings screen, so the same knobs
        // behave identically whether opened before or during a run
        private readonly SettingsMenu _settings;

        public OverworldMenu(Game1 game, Texture2D pixel, SpriteFont font)
        {
            _pixel    = pixel;
            _font     = font;
            _settings = new SettingsMenu(game);
        }

        public void Open()
        {
            IsActive     = true;
            _state       = MenuState.Main;
            _mainIndex   = 0;
            _itemIndex   = 0;
            _actionIndex = 0;
        }

        public void Close()
        {
            IsActive = false;
            _state   = MenuState.Main;
        }

        // onMessage hands text back to the caller to show in the dialogue box
        // (item used, item dropped, item description). The caller closes us.
        public void Update(InputManager input, PlayerData playerData, Action<string> onMessage = null)
        {
            if (!IsActive) return;

            switch (_state)
            {
                case MenuState.Main:
                    UpdateMain(input);
                    break;

                case MenuState.Item:
                    UpdateItem(input, playerData, onMessage);
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

                case MenuState.Settings:
                    UpdateSettings(input);
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
                SoundManager.Play(SoundManager.MenuMove);
                _mainIndex = (_mainIndex - 1 + _mainOptions.Length) % _mainOptions.Length;
            }
            else if (input.IsKeyPressed(Keys.Down))
            {
                SoundManager.Play(SoundManager.MenuMove);
                _mainIndex = (_mainIndex + 1) % _mainOptions.Length;
            }

            // Proceed with Z or Enter
            if (input.IsKeyPressed(Keys.Z) || input.IsKeyPressed(Keys.Enter))
            {
                SoundManager.Play(SoundManager.MenuSelect);
                if (_mainIndex == 0) // ITEM
                {
                    _state       = MenuState.Item;
                    _itemIndex   = 0;
                    _actionIndex = 0;
                }
                else if (_mainIndex == 1) // STAT
                {
                    _state = MenuState.Stat;
                }
                else if (_mainIndex == 2) // CELL
                {
                    _state = MenuState.Cell;
                }
                else if (_mainIndex == 3) // SETTINGS
                {
                    _state = MenuState.Settings;
                    _settings.ResetSelection();
                }
            }
        }

        private void UpdateSettings(InputManager input)
        {
            if (input.IsKeyPressed(Keys.X) || input.IsKeyPressed(Keys.C) || input.IsKeyPressed(Keys.Escape))
            {
                SoundManager.Play(SoundManager.MenuSelect);
                _state = MenuState.Main;
                return;
            }

            if (input.IsKeyPressed(Keys.Up))    _settings.MoveUp();
            if (input.IsKeyPressed(Keys.Down))  _settings.MoveDown();
            if (input.IsKeyPressed(Keys.Left))  _settings.Adjust(-1);
            if (input.IsKeyPressed(Keys.Right)) _settings.Adjust(1);

            if (input.IsKeyPressed(Keys.Z) || input.IsKeyPressed(Keys.Enter))
            {
                SoundManager.Play(SoundManager.MenuSelect);
                if (_settings.SelectedIndex == SettingsMenu.BackIndex)
                    _state = MenuState.Main;
                else
                    _settings.Adjust(1);
            }
        }

        // Up/Down picks the item, Left/Right picks USE/INFO/DROP, Z runs it.
        private void UpdateItem(InputManager input, PlayerData playerData, Action<string> onMessage)
        {
            // Back out to main menu
            if (input.IsKeyPressed(Keys.X) || input.IsKeyPressed(Keys.C) || input.IsKeyPressed(Keys.Escape))
            {
                SoundManager.Play(SoundManager.MenuSelect);
                _state = MenuState.Main;
                return;
            }

            var inventory = playerData?.Inventory;
            if (inventory == null || inventory.Count == 0)
                return;

            if (input.IsKeyPressed(Keys.Up))
            {
                SoundManager.Play(SoundManager.MenuMove);
                _itemIndex = (_itemIndex - 1 + inventory.Count) % inventory.Count;
            }
            else if (input.IsKeyPressed(Keys.Down))
            {
                SoundManager.Play(SoundManager.MenuMove);
                _itemIndex = (_itemIndex + 1) % inventory.Count;
            }

            if (input.IsKeyPressed(Keys.Left))
            {
                SoundManager.Play(SoundManager.MenuMove);
                _actionIndex = (_actionIndex - 1 + _actionOptions.Length) % _actionOptions.Length;
            }
            else if (input.IsKeyPressed(Keys.Right))
            {
                SoundManager.Play(SoundManager.MenuMove);
                _actionIndex = (_actionIndex + 1) % _actionOptions.Length;
            }

            if (input.IsKeyPressed(Keys.Z) || input.IsKeyPressed(Keys.Enter))
            {
                SoundManager.Play(SoundManager.MenuSelect);
                ItemData item = inventory[_itemIndex];

                switch (_actionIndex)
                {
                    case 0: // USE — equips armor, spends anything else
                        if (item.IsEquipment)
                        {
                            playerData.EquipArmorFromInventory(_itemIndex);
                            onMessage?.Invoke($"* You equipped the {item.Name}.");
                            break;
                        }

                        if (item.HealAmount > 0)
                        {
                            playerData.Heal(item.HealAmount);
                            // same cue the battle menu plays (BattleState.UseItem)
                            SoundManager.Play(SoundManager.HealSound);
                        }
                        inventory.RemoveAt(_itemIndex);
                        onMessage?.Invoke($"* You used the {item.Name}.");
                        break;

                    case 1: // INFO — the item stays in the bag
                        onMessage?.Invoke($"* {item.Name} - {item.Description}");
                        return;

                    case 2: // DROP
                        inventory.RemoveAt(_itemIndex);
                        onMessage?.Invoke($"* You dropped the {item.Name}.");
                        break;
                }

                if (inventory.Count == 0)
                    _state = MenuState.Main;
                else if (_itemIndex >= inventory.Count)
                    _itemIndex = inventory.Count - 1;
            }
        }

        public void Draw(SpriteBatch spriteBatch, PlayerData playerData)
        {
            if (!IsActive) return;

            DrawNameBox(spriteBatch, playerData);
            DrawMainBox(spriteBatch);

            // The right panel only exists once you've entered a submenu.
            if (_state == MenuState.Main) return;

            DrawBorderedBox(spriteBatch, RightBox);

            switch (_state)
            {
                case MenuState.Item:
                    DrawItemSubmenu(spriteBatch, playerData);
                    break;

                case MenuState.Stat:
                    DrawStatSubmenu(spriteBatch, playerData);
                    break;

                case MenuState.Cell:
                    DrawCellSubmenu(spriteBatch);
                    break;

                case MenuState.Settings:
                    DrawSettingsSubmenu(spriteBatch);
                    break;
            }
        }

        private void DrawNameBox(SpriteBatch spriteBatch, PlayerData playerData)
        {
            DrawBorderedBox(spriteBatch, NameBox);

            string name  = playerData?.Name ?? "CLOVER";
            int    hp    = playerData?.CurrentHp ?? 20;
            int    maxHp = playerData?.MaxHp ?? 20;
            int    money = playerData?.Money ?? 0;

            DrawText(spriteBatch, name,
                new Vector2(NameBox.X + NameLabelX, NameBox.Y + NameHeadY), Color.White, HeadingScale);

            string[] labels = { "HP", "G" };
            string[] values = { $"{hp}/{maxHp}", money.ToString() };

            for (int i = 0; i < labels.Length; i++)
            {
                float y = NameBox.Y + NameStatY + i * NameStatGap;
                DrawText(spriteBatch, labels[i], new Vector2(NameBox.X + NameLabelX, y), Color.White, StatScale);
                DrawText(spriteBatch, values[i], new Vector2(NameBox.X + NameValueX, y), Color.White, StatScale);
            }
        }

        private void DrawMainBox(SpriteBatch spriteBatch)
        {
            DrawBorderedBox(spriteBatch, MenuBox);

            for (int i = 0; i < _mainOptions.Length; i++)
            {
                float y   = MenuBox.Y + MainOptionY + i * MainOptionGap;
                var   pos = new Vector2(MenuBox.X + MainOptionX, y);

                if (_state == MenuState.Main && i == _mainIndex)
                    DrawHeart(spriteBatch, MenuBox.X + MainHeartX, y, HeadingScale);

                DrawText(spriteBatch, _mainOptions[i], pos, Color.White, HeadingScale);
            }
        }

        private void DrawItemSubmenu(SpriteBatch spriteBatch, PlayerData playerData)
        {
            var inventory = playerData?.Inventory;
            int count     = inventory == null ? 0 : Math.Min(inventory.Count, PlayerData.InventoryCapacity);

            for (int i = 0; i < count; i++)
            {
                float y = RightBox.Y + ItemFirstY + i * ItemGap;

                if (i == _itemIndex)
                    DrawHeart(spriteBatch, RightBox.X + ItemHeartX, y, HeadingScale);

                DrawText(spriteBatch, inventory[i].Name,
                    new Vector2(RightBox.X + ItemTextX, y), Color.White, HeadingScale);
            }

            if (count == 0)
            {
                DrawText(spriteBatch, "* Empty.",
                    new Vector2(RightBox.X + ItemTextX, RightBox.Y + ItemFirstY), Color.Gray, HeadingScale);
            }

            // USE / INFO / DROP along the bottom of the panel, measured so the
            // three sit as one centred group instead of drifting apart
            float actionY = RightBox.Bottom - ActionBottomPad - _font.LineSpacing * HeadingScale;

            float groupWidth = ActionGap * (_actionOptions.Length - 1);
            foreach (string option in _actionOptions)
                groupWidth += _font.MeasureString(option).X * HeadingScale;

            float x = RightBox.Center.X - groupWidth / 2f;

            for (int i = 0; i < _actionOptions.Length; i++)
            {
                Color color = count > 0 && i == _actionIndex ? HighlightColor : Color.White;
                DrawText(spriteBatch, _actionOptions[i], new Vector2(x, actionY), color, HeadingScale);

                x += _font.MeasureString(_actionOptions[i]).X * HeadingScale + ActionGap;
            }
        }

        private void DrawStatSubmenu(SpriteBatch spriteBatch, PlayerData playerData)
        {
            string name  = playerData?.Name ?? "CLOVER";
            int    hp    = playerData?.CurrentHp ?? 20;
            int    maxHp = playerData?.MaxHp ?? 20;
            int    atk   = playerData?.Attack ?? 10;
            int    money = playerData?.Money ?? 0;

            float x = RightBox.X + ItemTextX;
            float y = RightBox.Y + ItemFirstY;

            DrawText(spriteBatch, $"\"{name}\"", new Vector2(x, y), Color.White, HeadingScale);

            // DF reads "base (from armor)", the way the reference shows it. The
            // armor's name goes on its own line — the panel is a third narrower
            // than the reference's and "ARMOR: <name>" runs past the border.
            string[] rows =
            {
                $"HP  {hp}/{maxHp}",
                "",
                $"AT  {atk}",
                $"DF  {playerData?.BaseDefense ?? 0} ({playerData?.ArmorDefense ?? 0})",
                "",
                "ARMOR:",
                $"  {playerData?.EquippedArmor?.Name ?? "None"}",
                "",
                $"GOLD  {money}"
            };

            for (int i = 0; i < rows.Length; i++)
            {
                if (rows[i].Length == 0) continue;
                DrawText(spriteBatch, rows[i],
                    new Vector2(x, y + ItemGap * (i + 1)), Color.White, HeadingScale);
            }
        }

        private void DrawCellSubmenu(SpriteBatch spriteBatch)
        {
            float x = RightBox.X + ItemTextX;
            float y = RightBox.Y + ItemFirstY;

            DrawText(spriteBatch, "CELL", new Vector2(x, y), Color.White, HeadingScale);
            DrawText(spriteBatch, "* No response...",
                new Vector2(x, y + ItemGap * 2), Color.Gray, HeadingScale);
        }

        private void DrawSettingsSubmenu(SpriteBatch spriteBatch)
        {
            for (int i = 0; i < SettingsMenu.Labels.Length; i++)
            {
                float y = RightBox.Y + ItemFirstY + i * ItemGap;

                if (i == _settings.SelectedIndex)
                    DrawHeart(spriteBatch, RightBox.X + ItemHeartX, y, HeadingScale);

                DrawText(spriteBatch, _settings.DisplayFor(i),
                    new Vector2(RightBox.X + ItemTextX, y), Color.White, HeadingScale);
            }
        }

        // The red soul, vertically centred against a text row starting at rowY.
        // Falls back to a ">" if the sprite isn't loaded.
        private void DrawHeart(SpriteBatch spriteBatch, float x, float rowY, float textScale)
        {
            float rowHeight = _font.LineSpacing * textScale;
            Spritesheet soul = SpriteManager.GetSprite("soul");

            if (soul?.Texture == null)
            {
                DrawText(spriteBatch, ">", new Vector2(x, rowY), Color.Red, textScale);
                return;
            }

            Rectangle src = soul[0, 0];
            var dest = new Rectangle(
                (int)x,
                (int)(rowY + (rowHeight - src.Height) / 2f),
                src.Width, src.Height);

            spriteBatch.Draw(soul.Texture, dest, src, Color.White);
        }

        private void DrawBorderedBox(SpriteBatch spriteBatch, Rectangle rect)
        {
            const int t = BorderThickness;

            spriteBatch.Draw(_pixel, rect, Color.Black);
            spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, rect.Width, t), Color.White);
            spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Bottom - t, rect.Width, t), Color.White);
            spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, t, rect.Height), Color.White);
            spriteBatch.Draw(_pixel, new Rectangle(rect.Right - t, rect.Y, t, rect.Height), Color.White);
        }

        private void DrawText(SpriteBatch spriteBatch, string text, Vector2 position, Color color, float scale)
        {
            spriteBatch.DrawString(_font, text, position, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }
    }
}
