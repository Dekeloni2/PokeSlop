namespace FinalProject.Core;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public class Text : IGameDrawable
{
    public string Content { get; set; }
    public string FontName { get; set; }
    public Vector2 Position { get; set; }
    public Color Color { get; set; }
    public float Scale { get; set; }

    private SpriteFont _font;

    public Text(string fontName, string content, Vector2 position, Color color, float scale = 1f)
    {
        FontName = fontName;
        Content = content;
        Position = position;
        Color = color;
        Scale = scale;
        _font = null;
    }

    public void SetFont(SpriteFont font)
    {
        _font = font;
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        if (_font == null) return;
        spriteBatch.DrawString(_font, Content, Position, Color, 0f, Vector2.Zero, Scale, SpriteEffects.None, 0f);
    }
}
