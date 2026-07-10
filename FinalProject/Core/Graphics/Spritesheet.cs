namespace FinalProject.Core.Graphics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

// a texture split into equal frames. sheet[x, y] gives the source rect,
// spacing is for atlases that have gaps between the cells.
public class Spritesheet
{
    public int       Columns  { get; set; }
    public int       Rows     { get; set; }
    public Texture2D Texture  { get; set; }
    public int       SpacingX { get; set; } // gutter between columns, in px
    public int       SpacingY { get; set; } // gutter between rows, in px

    public Rectangle this[int x, int y]
    {
        get
        {
            int width  = (Texture.Width  - SpacingX * (Columns - 1)) / Columns;
            int height = (Texture.Height - SpacingY * (Rows    - 1)) / Rows;

            return new Rectangle(x * (width + SpacingX), y * (height + SpacingY), width, height);
        }
    }
}
