using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FinalProject.World
{
    // A loaded map with all its layers and tilesets.
    // Handles drawing and collision queries.
    public class TileMap
    {
        public int Width      { get; }  // in tiles
        public int Height     { get; }  // in tiles
        public int TileWidth  { get; }  // in pixels
        public int TileHeight { get; }  // in pixels

        // Pixel dimensions of the full map
        public int PixelWidth  => Width  * TileWidth;
        public int PixelHeight => Height * TileHeight;

        private readonly List<TilesetInfo> _tilesets;
        private readonly TileLayer         _groundLayer;
        private readonly TileLayer         _tallGrassLayer;
        private readonly TileLayer         _objectsLayer;

        public TileMap(int width, int height, int tileWidth, int tileHeight,
                       List<TilesetInfo> tilesets,
                       TileLayer groundLayer, TileLayer tallGrassLayer, TileLayer objectsLayer)
        {
            Width         = width;
            Height        = height;
            TileWidth     = tileWidth;
            TileHeight    = tileHeight;
            _tilesets     = tilesets;
            _groundLayer  = groundLayer;
            _tallGrassLayer = tallGrassLayer;
            _objectsLayer = objectsLayer;
        }

        public bool IsInBounds(int tileX, int tileY)
            => tileX >= 0 && tileY >= 0 && tileX < Width && tileY < Height;

        // Returns true if the player can walk onto this tile
        public bool IsWalkable(int tileX, int tileY)
        {
            // Out of bounds is never walkable
            if (!_groundLayer.InBounds(tileX, tileY))
                return false;

            // Any tile on the Objects layer blocks movement
            return !_objectsLayer.HasTile(tileX, tileY);
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            // Draw order: ground, tall grass (if any), then objects
            DrawLayer(_groundLayer,  spriteBatch);
            if (_tallGrassLayer != null)
                DrawLayer(_tallGrassLayer, spriteBatch);
            DrawLayer(_objectsLayer, spriteBatch);
        }

        // Returns true if the given tile coordinate contains tall grass
        public bool IsTallGrass(int tileX, int tileY)
        {
            if (_tallGrassLayer == null) return false;
            if (!_tallGrassLayer.InBounds(tileX, tileY)) return false;
            return _tallGrassLayer.HasTile(tileX, tileY);
        }

        private void DrawLayer(TileLayer layer, SpriteBatch spriteBatch)
        {
            for (int y = 0; y < layer.Height; y++)
            {
                for (int x = 0; x < layer.Width; x++)
                {
                    int gid = layer.GetTileGid(x, y);
                    if (gid == 0) continue;

                    TilesetInfo tileset = FindTileset(gid);
                    if (tileset == null) continue;

                    Rectangle src = tileset.GetSourceRect(gid);
                    Rectangle dst = new Rectangle(
                        x * TileWidth,
                        y * TileHeight,
                        TileWidth,
                        TileHeight
                    );

                    spriteBatch.Draw(tileset.Texture, dst, src, Color.White);
                }
            }
        }

        // Finds the tileset that owns a given global tile ID.
        // Tilesets are sorted by firstGid ascending — the owner is the one
        // with the highest firstGid that is still <= the given gid.
        private TilesetInfo FindTileset(int gid)
        {
            TilesetInfo result = null;

            foreach (TilesetInfo ts in _tilesets)
            {
                if (ts.FirstGid <= gid)
                    result = ts;
                else
                    break;
            }

            return result;
        }
    }
}