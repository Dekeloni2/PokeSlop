using System.Collections.Generic;
using FinalProject.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using FinalProject.Core.Audio;
using FinalProject.Core.Input;
using FinalProject.Data;

namespace FinalProject.UI;

public class VendingMachineMenu
{
    private readonly Texture2D _pixel;
    private readonly SpriteFont _font;

    public bool IsActive { get; private set; }
    private int _selectedIndex = 0;
    private string _statusMessage = "* Insert Gold.";
    private List<ItemData> _items;
    
    public VendingMachineMenu(Texture2D pixel, SpriteFont font)
    {
        _pixel = pixel;
        _font = font;
    }
    
    public void Open(List<ItemData> items)
    {
        IsActive = true;
        _items = items;
        _selectedIndex = 0;
        _statusMessage = "* Insert Gold.";
    }
    
    public void Close() => IsActive = false;

    public void Update(InputManager input, PlayerData playerData)
    {
        if (!IsActive || _items == null || _items.Count == 0) return;

        // Close machine with X or Esc
        if (input.IsKeyPressed(Keys.X) || input.IsKeyPressed(Keys.Escape))
        {
            Close();
            return;
        }

        // Scroll items
        if (input.IsKeyPressed(Keys.Up))
        {
            SoundManager.Play(SoundManager.MenuMove);
            _selectedIndex = (_selectedIndex - 1 + _items.Count) % _items.Count;
        }
        else if (input.IsKeyPressed(Keys.Down))
        {
            SoundManager.Play(SoundManager.MenuMove);
            _selectedIndex = (_selectedIndex + 1) % _items.Count;
        }

        // Buy item with Z or Enter
        if (input.IsKeyPressed(Keys.Z) || input.IsKeyPressed(Keys.Enter))
        {
            ItemData selectedItem = _items[_selectedIndex];

            if (playerData.Money >= selectedItem.Price)
            {
                SoundManager.Play(SoundManager.MenuSelect);
                playerData.Money -= selectedItem.Price;
                playerData.Inventory.Add(selectedItem);
                _statusMessage = $"* *CLINK!* Got {selectedItem.Name}.";
            }
            else
            {
                _statusMessage = "* Not enough Gold!";
            }
        }
    }
    
    public void Draw(SpriteBatch spriteBatch, PlayerData playerData)
    {
        if (!IsActive) return;

        // Machine Frame Box
        Rectangle box = new Rectangle(30, 30, 400, 240);
        DrawBorderedBox(spriteBatch, box, Color.Black, Color.White, 3);

        // Status Line & Current Gold
        DrawText(spriteBatch, _statusMessage, new Vector2(box.X + 15, box.Y + 12), Color.White, 2f);
        DrawText(spriteBatch, $"G: {playerData?.Money ?? 0}", new Vector2(box.X + 230, box.Y + 12), Color.Yellow, 2f);

        // Divider Line
        spriteBatch.Draw(_pixel, new Rectangle(box.X + 10, box.Y + 38, box.Width - 20, 2), Color.Gray);

        // Item Selection List
        for (int i = 0; i < _items.Count; i++)
        {
            Vector2 pos = new Vector2(box.X + 35, box.Y + 50 + (i * 26));

            if (i == _selectedIndex)
            {
                DrawText(spriteBatch, ">", new Vector2(pos.X - 15, pos.Y), Color.Red);
            }

            ItemData item = _items[i];
            DrawText(spriteBatch, item.Name, pos, Color.White);
            DrawText(spriteBatch, $"{item.Price}G", new Vector2(box.X + 230, pos.Y), Color.Gold);
        }

        // Exit hint
        DrawText(spriteBatch, "[X] Leave Machine", new Vector2(box.X + 15, box.Bottom - 22), Color.Gray, 2f);
    }
    
    private void DrawBorderedBox(SpriteBatch spriteBatch, Rectangle rect, Color bgColor, Color borderColor, int thickness)
    {
        spriteBatch.Draw(_pixel, rect, bgColor);
        spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), borderColor);
        spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), borderColor);
        spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), borderColor);
        spriteBatch.Draw(_pixel, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), borderColor);
    }

    private void DrawText(SpriteBatch spriteBatch, string text, Vector2 position, Color color, float scale = 2f)
    {
        spriteBatch.DrawString(_font, text, position, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
    }
}