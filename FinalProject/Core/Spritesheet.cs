namespace FinalProject.Core;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

// A texture cut into a uniform Columns x Rows grid of equal-size frames.
// Usage: sheet[2, 0] -> the source rect for column 2, row 0.
public class Spritesheet
{
    public int       Columns { get; set; }
    public int       Rows    { get; set; }
    public Texture2D Texture { get; set; }

    public Rectangle this[int x, int y]
    {
        get
        {
            int width  = Texture.Width  / Columns;
            int height = Texture.Height / Rows;

            return new Rectangle(width * x, height * y, width, height);
        }
    }
}
