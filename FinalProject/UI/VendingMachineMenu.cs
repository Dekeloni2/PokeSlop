using System;
using System.Collections.Generic;
using FinalProject.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using FinalProject.Core.Audio;
using FinalProject.Core.Graphics;
using FinalProject.Core.Input;
using FinalProject.Core.Text;
using FinalProject.Data;

namespace FinalProject.UI;

// The shop, laid out like Undertale's but scaled down — it opens over the
// world rather than taking the screen, and the vendor is an ordinary overworld
// NPC, so there's no portrait panel. That leaves the item list on the left, an
// info panel top-right that slides in, and gold/status underneath it.
public class VendingMachineMenu
{
    // ── Layout ───────────────────────────────────────────────────────────────
    // One centred block. Move or resize it by editing these five numbers.
    // The block fills the bottom half of the screen, edge to edge, with the
    // reference's ~2:1 split between the item list and the side column.
    private const int Margin      = 8;
    private const int BoxGap      = 8;
    private const int BorderThickness = 5;

    private const int BlockHeight = 232;
    private const int ListWidth   = 464;
    private const int InfoHeight  = 124;

    private static readonly int BlockX     = Margin;
    private static readonly int BlockY     = GameSettings.WindowHeight - BlockHeight - Margin;
    private static readonly int BlockWidth = GameSettings.WindowWidth - Margin * 2;

    private static readonly Rectangle ListBox = new(BlockX, BlockY, ListWidth, BlockHeight);

    private static readonly int SideX     = BlockX + ListWidth + BoxGap;
    private static readonly int SideWidth = BlockWidth - ListWidth - BoxGap;

    // Rest positions. The info panel starts below the screen and rises into
    // the top one; the gold box is drawn after it, so it passes behind.
    private static readonly Rectangle InfoBox = new(SideX, BlockY, SideWidth, InfoHeight);
    private static readonly Rectangle GoldBox = new(SideX, BlockY + InfoHeight + BoxGap,
        SideWidth, BlockHeight - InfoHeight - BoxGap);

    private const float SlideSeconds = 0.35f;

    // ── Text ─────────────────────────────────────────────────────────────────
    private const float RowScale  = 2f;
    // The side column is a third of the width, so at the block's reduced size a
    // full description only fits at the smaller scale. The gold line stays at 2.
    private const float InfoScale = 1.5f;
    private const float GoldScale = 2f;

    private const int PadX = 14;
    private const int PadY = 16;
    private const int RowGap = 30;
    private const int HeartX = 12;
    private const int RowTextX = 34;

    private const int ItemsPerPage = 4;

    // the info box fits 5 lines; the gold box fits 2 above its pinned bottom line
    private const int InfoMaxLines   = 5;
    private const int StatusMaxLines = 2;

    private readonly Texture2D  _pixel;
    private readonly SpriteFont _font;

    public bool IsActive { get; private set; }

    private List<ItemData> _items;
    private int   _selectedIndex;   // index into the current page; == page size means "Exit"
    private int   _page;
    private float _slideTimer;
    private string _statusMessage = "";

    public VendingMachineMenu(Texture2D pixel, SpriteFont font)
    {
        _pixel = pixel;
        _font  = font;
    }

    public void Open(List<ItemData> items)
    {
        IsActive       = true;
        _items         = items;
        _selectedIndex = 0;
        _page          = 0;
        _slideTimer    = 0f;
        _statusMessage = "";
    }

    public void Close() => IsActive = false;

    // ── Paging ───────────────────────────────────────────────────────────────

    private int ItemCount => _items?.Count ?? 0;

    private int PageCount => Math.Max(1, (ItemCount + ItemsPerPage - 1) / ItemsPerPage);

    // how many item rows this page actually has (the last page may be short)
    private int RowsOnPage => Math.Min(ItemsPerPage, Math.Max(0, ItemCount - _page * ItemsPerPage));

    // Exit sits below the items, so it's one past the last item row.
    private int ExitRow => RowsOnPage;

    private bool ExitSelected => _selectedIndex >= ExitRow;

    private ItemData SelectedItem
        => ExitSelected ? null : _items[_page * ItemsPerPage + _selectedIndex];

    // ── Update ───────────────────────────────────────────────────────────────

    public void Update(GameTime gameTime, InputManager input, PlayerData playerData)
    {
        if (!IsActive) return;

        if (_slideTimer < SlideSeconds)
            _slideTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (input.IsKeyPressed(Keys.X) || input.IsKeyPressed(Keys.Escape))
        {
            Close();
            return;
        }

        if (input.IsKeyPressed(Keys.Up))
        {
            SoundManager.Play(SoundManager.MenuMove);
            _selectedIndex = (_selectedIndex - 1 + ExitRow + 1) % (ExitRow + 1);
        }
        else if (input.IsKeyPressed(Keys.Down))
        {
            SoundManager.Play(SoundManager.MenuMove);
            _selectedIndex = (_selectedIndex + 1) % (ExitRow + 1);
        }

        // Left/Right page through the stock. Only bound when there's more than
        // one page, so a small shop doesn't eat the keys silently.
        if (PageCount > 1)
        {
            if (input.IsKeyPressed(Keys.Left))
            {
                SoundManager.Play(SoundManager.MenuMove);
                ChangePage(-1);
            }
            else if (input.IsKeyPressed(Keys.Right))
            {
                SoundManager.Play(SoundManager.MenuMove);
                ChangePage(1);
            }
        }

        if (input.IsKeyPressed(Keys.Z) || input.IsKeyPressed(Keys.Enter))
            Confirm(playerData);
    }

    private void ChangePage(int delta)
    {
        _page = (_page + delta + PageCount) % PageCount;

        // the new page may be shorter than where the cursor was sitting
        _selectedIndex = Math.Min(_selectedIndex, ExitRow);
    }

    private void Confirm(PlayerData playerData)
    {
        if (ExitSelected)
        {
            SoundManager.Play(SoundManager.MenuSelect);
            Close();
            return;
        }

        ItemData item = SelectedItem;

        // kept short — the gold box only has room for two lines above its
        // pinned gold/bag line
        if (playerData.IsInventoryFull)
        {
            _statusMessage = "Bag is full.";
            return;
        }

        if (playerData.Money < item.Price)
        {
            _statusMessage = "Not enough gold.";
            return;
        }

        SoundManager.Play(SoundManager.MenuSelect);
        playerData.Money -= item.Price;
        playerData.Inventory.Add(item);
        _statusMessage = "Thanks!";
    }

    // ── Draw ─────────────────────────────────────────────────────────────────

    public void Draw(SpriteBatch spriteBatch, PlayerData playerData)
    {
        if (!IsActive) return;

        DrawList(spriteBatch);

        // info first, so the gold box occludes it on the way up
        DrawInfo(spriteBatch);
        DrawGold(spriteBatch, playerData);
    }

    private void DrawList(SpriteBatch spriteBatch)
    {
        DrawBorderedBox(spriteBatch, ListBox);

        int first = _page * ItemsPerPage;

        for (int i = 0; i < RowsOnPage; i++)
        {
            ItemData item = _items[first + i];
            float    y    = ListBox.Y + PadY + i * RowGap;

            if (i == _selectedIndex)
                DrawHeart(spriteBatch, ListBox.X + HeartX, y, RowScale);

            DrawText(spriteBatch, $"{item.Price}G - {item.Name}",
                new Vector2(ListBox.X + RowTextX, y), Color.White, RowScale);
        }

        // Exit always sits one row below the last item, so its position is
        // stable even on a short final page.
        float exitY = ListBox.Y + PadY + ExitRow * RowGap;

        if (ExitSelected)
            DrawHeart(spriteBatch, ListBox.X + HeartX, exitY, RowScale);

        DrawText(spriteBatch, "Exit", new Vector2(ListBox.X + RowTextX, exitY), Color.White, RowScale);

        if (PageCount > 1)
        {
            string indicator = $"< {_page + 1}/{PageCount} >";
            Vector2 size = _font.MeasureString(indicator) * RowScale;
            DrawText(spriteBatch, indicator,
                new Vector2(ListBox.Center.X - size.X / 2f, ListBox.Bottom - PadY - size.Y),
                Color.Gray, RowScale);
        }
    }

    // Rises from below the screen into place, passing behind the gold box on
    // the way. SmoothStep eases out at the end so it settles rather than
    // slamming home — nothing plays on arrival.
    private void DrawInfo(SpriteBatch spriteBatch)
    {
        float t = MathHelper.Clamp(_slideTimer / SlideSeconds, 0f, 1f);
        int   y = (int)MathHelper.SmoothStep(GameSettings.WindowHeight, InfoBox.Y, t);

        var rect = new Rectangle(InfoBox.X, y, InfoBox.Width, InfoBox.Height);
        DrawBorderedBox(spriteBatch, rect);

        ItemData item = SelectedItem;
        string body;

        if (item == null)               body = "Come again.";
        else if (item.IsEquipment)      body = $"Armor\nDF +{item.Defense}\n({item.Description})";
        else if (item.HealAmount > 0)   body = $"Heals {item.HealAmount}HP\n({item.Description})";
        else                            body = $"({item.Description})";

        DrawWrapped(spriteBatch, body, rect, InfoScale, InfoMaxLines);
    }

    private void DrawGold(SpriteBatch spriteBatch, PlayerData playerData)
    {
        DrawBorderedBox(spriteBatch, GoldBox);

        if (_statusMessage.Length > 0)
            DrawWrapped(spriteBatch, _statusMessage, GoldBox, InfoScale, StatusMaxLines);

        // gold and bag count on the bottom line, like the reference
        int money = playerData?.Money ?? 0;
        int held  = playerData?.Inventory.Count ?? 0;

        string gold = $"{money}G";
        string bag  = $"{held}/{PlayerData.InventoryCapacity}";

        float lineY = GoldBox.Bottom - PadY - _font.LineSpacing * GoldScale;
        Vector2 bagSize = _font.MeasureString(bag) * GoldScale;

        DrawText(spriteBatch, gold, new Vector2(GoldBox.X + PadX, lineY), Color.White, GoldScale);
        DrawText(spriteBatch, bag,
            new Vector2(GoldBox.Right - PadX - bagSize.X, lineY), Color.White, GoldScale);
    }

    // maxLines keeps a long description from running out of its box and, in the
    // gold box, from colliding with the gold/bag line pinned to the bottom.
    private void DrawWrapped(SpriteBatch spriteBatch, string text, Rectangle rect, float scale, int maxLines)
    {
        List<string> lines = TextWrap.ToLines(_font, text, rect.Width - PadX * 2, scale);
        float lineHeight = _font.LineSpacing * scale;

        for (int i = 0; i < Math.Min(lines.Count, maxLines); i++)
        {
            DrawText(spriteBatch, lines[i],
                new Vector2(rect.X + PadX, rect.Y + PadY + i * lineHeight), Color.White, scale);
        }
    }

    // Same red soul the overworld menu and battle buttons use.
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
