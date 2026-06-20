// World/TilesetInfo.cs
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

        public TilesetInfo(int firstGid, Texture2D texture, int columns, int tileWidth, int tileHeight)
        {
            FirstGid   = firstGid;
            Texture    = texture;
            Columns    = columns;
            TileWidth  = tileWidth;
            TileHeight = tileHeight;
        }

        // Returns the source rectangle in the texture for a given global tile ID
        public Rectangle GetSourceRect(int globalId)
        {
            int localId = globalId - FirstGid;
            int col     = localId % Columns;
            int row     = localId / Columns;

            return new Rectangle(col * TileWidth, row * TileHeight, TileWidth, TileHeight);
        }
    }
}