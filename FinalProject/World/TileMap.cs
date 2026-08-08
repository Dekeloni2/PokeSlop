// World/TileMap.cs
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FinalProject.Core;
using FinalProject.Core.Graphics;

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
        private readonly TileLayer         _objectsLayer;
        private readonly TileLayer         _elevatorDoorLayer;
        private readonly List<Interactable> _interactables;
        private readonly List<Npc>     _npcs;

        // tiles blocked by something that isn't baked into the Objects layer —
        // right now just NPCs that haven't been resolved yet. OverworldState
        // keeps this in sync with RouteTracker every time it loads or resumes.
        private readonly HashSet<Point> _blockedTiles = new();

        public IReadOnlyList<Npc> Npcs => _npcs;

        public void SetTileBlocked(int tileX, int tileY, bool blocked)
        {
            var tile = new Point(tileX, tileY);
            if (blocked) _blockedTiles.Add(tile);
            else         _blockedTiles.Remove(tile);
        }

        // Optional overlay layer drawn on top of the map — the elevator door.
        // Flip ElevatorDoorVisible to "close"/"open" it without swapping maps.
        // HasElevatorDoor is false on maps that don't define the layer.
        public bool ElevatorDoorVisible { get; set; } = false;
        public bool HasElevatorDoor => _elevatorDoorLayer != null;

        public TileMap(int width, int height, int tileWidth, int tileHeight,
                       List<TilesetInfo> tilesets,
                       TileLayer groundLayer, TileLayer objectsLayer,
                       TileLayer elevatorDoorLayer = null,
                       List<Interactable> interactables = null,
                       List<Npc> npcs = null)
        {
            Width         = width;
            Height        = height;
            TileWidth     = tileWidth;
            TileHeight    = tileHeight;
            _tilesets     = tilesets;
            _groundLayer  = groundLayer;
            _objectsLayer = objectsLayer;
            _elevatorDoorLayer = elevatorDoorLayer;
            _interactables = interactables ?? new List<Interactable>();
            _npcs          = npcs ?? new List<Npc>();
        }

        // true on the tiles the elevator door covers, used to catch the player
        // walking out of the lift
        public bool IsElevatorDoorTile(int tileX, int tileY)
            => _elevatorDoorLayer != null
               && _elevatorDoorLayer.InBounds(tileX, tileY)
               && _elevatorDoorLayer.HasTile(tileX, tileY);

        public bool IsInBounds(int tileX, int tileY)
            => tileX >= 0 && tileY >= 0 && tileX < Width && tileY < Height;

        // Returns true if the player can walk onto this tile
        public bool IsWalkable(int tileX, int tileY)
        {
            // Out of bounds is never walkable
            if (!_groundLayer.InBounds(tileX, tileY))
                return false;

            if (_blockedTiles.Contains(new Point(tileX, tileY)))
                return false;

            // Any tile on the Objects layer blocks movement
            return !_objectsLayer.HasTile(tileX, tileY);
        }

        // Draw only the tiles visible through the camera viewport.
        public void Draw(SpriteBatch spriteBatch, Camera camera)
        {
            // Convert the camera's world-space position to a visible tile range.
            // Add one tile of padding on each edge to avoid pop-in during sub-tile scrolling.
            float viewW = GameSettings.WindowWidth  / GameSettings.Zoom;
            float viewH = GameSettings.WindowHeight / GameSettings.Zoom;

            int minX = Math.Max(0,      (int)(camera.Position.X / TileWidth)  - 1);
            int minY = Math.Max(0,      (int)(camera.Position.Y / TileHeight) - 1);
            int maxX = Math.Min(Width,  (int)((camera.Position.X + viewW) / TileWidth)  + 2);
            int maxY = Math.Min(Height, (int)((camera.Position.Y + viewH) / TileHeight) + 2);

            // Draw order: ground, then objects
            DrawLayer(_groundLayer,   spriteBatch, minX, minY, maxX, maxY);
            DrawLayer(_objectsLayer,  spriteBatch, minX, minY, maxX, maxY);

            // the elevator door sits on top of the map; hide it to "open" the door
            if (_elevatorDoorLayer != null && ElevatorDoorVisible)
                DrawLayer(_elevatorDoorLayer, spriteBatch, minX, minY, maxX, maxY);
        }
        

        // Returns the interactable covering the given tile (e.g. the tile the
        // player is facing), or null if there isn't one there.
        public Interactable GetInteractableAt(int tileX, int tileY)
        {
            foreach (Interactable interactable in _interactables)
                if (interactable.ContainsTile(tileX, tileY))
                    return interactable;
            return null;
        }

        // Reads/overwrites a single Objects-layer cell at runtime — for a
        // one-off visual change tied to game state rather than a whole extra
        // overlay layer (the elevator door's approach). The cheese stand
        // swapping to its empty tile once picked up is the first user of this.
        public int GetObjectsTileGid(int tileX, int tileY)
            => _objectsLayer.InBounds(tileX, tileY) ? _objectsLayer.GetTileGid(tileX, tileY) : 0;

        public void SetObjectsTile(int tileX, int tileY, int gid)
            => _objectsLayer.SetTileGid(tileX, tileY, gid);

        private void DrawLayer(TileLayer layer, SpriteBatch spriteBatch,
                                int minX, int minY, int maxX, int maxY)
        {
            for (int y = minY; y < maxY; y++)
            {
                for (int x = minX; x < maxX; x++)
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
