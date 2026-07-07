namespace FinalProject.Core;

using System.Collections.Generic;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

public class SpriteManager
{
    private static readonly Dictionary<string, Spritesheet> _sprites = new();

    private static ContentManager _content;

    public SpriteManager(ContentManager content)
    {
        _content = content;
    }

    public static void AddSprite(string spriteName, string fileName, int columns = 1, int rows = 1)
    {
        _sprites[spriteName] = new Spritesheet
        {
            Texture = _content.Load<Texture2D>(fileName),
            Columns = columns,
            Rows    = rows
        };
    }

    public static Spritesheet GetSprite(string spriteName)
        => _sprites.TryGetValue(spriteName, out Spritesheet sheet) ? sheet : null;
}
