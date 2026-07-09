
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FinalProject.World
{
    // Represents one tileset used by a map.
    // A map can reference multiple tilesets — each has a firstGid that marks
    // where its tile IDs begin in the global ID space.
    public class TilesetInfo
    {
        public int       FirstGid   { get; }
        public Texture2D Texture    { get; }
        public int       Columns    { get; }
        public int       TileWidth  { get; }
        public int       TileHeight { get; }
        public int       Spacing    { get; } // gutter between tiles, in px
        public int       Margin     { get; } // border around the whole atlas, in px

        public TilesetInfo(int firstGid, Texture2D texture, int columns, int tileWidth, int tileHeight,
            int spacing = 0, int margin = 0)
        {
            FirstGid   = firstGid;
            Texture    = texture;
            Columns    = columns;
            TileWidth  = tileWidth;
            TileHeight = tileHeight;
            Spacing    = spacing;
            Margin     = margin;
        }

        // Returns the source rectangle in the texture for a given global tile ID.
        // Accounts for the atlas margin and the spacing (gutter) between tiles —
        // without these, tiles are sampled progressively off toward the bottom-right.
        public Rectangle GetSourceRect(int globalId)
        {
            int localId = globalId - FirstGid;
            int col     = localId % Columns;
            int row     = localId / Columns;

            int x = Margin + col * (TileWidth  + Spacing);
            int y = Margin + row * (TileHeight + Spacing);

            return new Rectangle(x, y, TileWidth, TileHeight);
        }
    }
}