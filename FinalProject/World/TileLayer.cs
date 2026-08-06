// World/TileLayer.cs
namespace FinalProject.World
{
    // Stores the tile ID data for one layer (Ground, Objects, etc.)
    // Tiled exports each layer as a flat array, left to right, top to bottom.
    // Index formula: data[y * width + x]
    public class TileLayer
    {
        public string Name   { get; }
        public int    Width  { get; }
        public int    Height { get; }

        private readonly int[] _data;

        public TileLayer(string name, int width, int height, int[] data)
        {
            Name   = name;
            Width  = width;
            Height = height;
            _data  = data;
        }

        // Returns the global tile ID at a tile coordinate. 0 means empty.
        public int GetTileGid(int tileX, int tileY)
            => _data[tileY * Width + tileX];

        // True if there is any tile at this coordinate
        public bool HasTile(int tileX, int tileY)
            => GetTileGid(tileX, tileY) != 0;

        // True if the coordinate is within the layer bounds
        public bool InBounds(int tileX, int tileY)
            => tileX >= 0 && tileY >= 0 && tileX < Width && tileY < Height;

        // Overwrites one cell's tile ID at runtime — the cheese pickup swapping
        // its plate to the empty variant, say. The array itself is mutable even
        // though the field holding it is readonly; only out-of-bounds is guarded.
        public void SetTileGid(int tileX, int tileY, int gid)
        {
            if (!InBounds(tileX, tileY)) return;
            _data[tileY * Width + tileX] = gid;
        }
    }
}