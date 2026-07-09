// Core/Camera.cs
using System;
using Microsoft.Xna.Framework;
using FinalProject.Entities;
using FinalProject.World;

namespace FinalProject.Core.Graphics
{
    // Follows the player and produces a transform matrix that offsets
    // everything drawn so the player stays centered on screen.
    // Clamps to map bounds so the camera never shows outside the map.
    public class Camera
    {
        public Vector2 Position { get; private set; }

        private readonly int _halfWidth;
        private readonly int _halfHeight;

        public Camera()
        {
            _halfWidth  = GameSettings.WindowWidth  / 2;
            _halfHeight = GameSettings.WindowHeight / 2;
        }

        // Call this every frame before drawing.
        // Pass the current map so the camera can clamp to its edges.
        public void Follow(Player player, TileMap map)
        {
            // How many world pixels fit in the viewport at the current zoom level
            float viewW = GameSettings.WindowWidth  / GameSettings.Zoom;
            float viewH = GameSettings.WindowHeight / GameSettings.Zoom;

            // Center the viewport on the player's tile
            float x = player.WorldPosition.X - viewW / 2f + GameSettings.TileSize / 2f;
            float y = player.WorldPosition.Y - viewH / 2f + GameSettings.TileSize / 2f;

            // Clamp so we never reveal world space outside the map
            x = MathHelper.Clamp(x, 0, Math.Max(0f, map.PixelWidth  - viewW));
            y = MathHelper.Clamp(y, 0, Math.Max(0f, map.PixelHeight - viewH));

            Position = new Vector2(x, y);
        }

        public Matrix GetTransform()
        {
            // Translate so the camera position is at screen origin, then scale up
            return Matrix.CreateTranslation(-Position.X, -Position.Y, 0f)
                 * Matrix.CreateScale(GameSettings.Zoom, GameSettings.Zoom, 1f);
        }
    }
}