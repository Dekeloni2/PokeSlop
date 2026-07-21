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

        // Call this every frame before drawing.
        // Pass the current map so the camera can clamp to its edges.
        public void Follow(Player player, TileMap map)
        {
            // How many world pixels fit in the viewport at the current zoom level
            float viewW = GameSettings.WindowWidth  / GameSettings.Zoom;
            float viewH = GameSettings.WindowHeight / GameSettings.Zoom;

            // Center the viewport on the player's tile
            float x = player.WorldPosition.X - viewW / 2f + map.TileWidth  / 2f;
            float y = player.WorldPosition.Y - viewH / 2f + map.TileHeight / 2f;

            // If the map is smaller than the viewport on an axis, center it (the
            // camera sits at a negative offset) instead of pinning it to the
            // top-left corner. Otherwise clamp so we never scroll past the edges.
            x = map.PixelWidth  <= viewW ? (map.PixelWidth  - viewW) / 2f
                                         : MathHelper.Clamp(x, 0, map.PixelWidth  - viewW);
            y = map.PixelHeight <= viewH ? (map.PixelHeight - viewH) / 2f
                                         : MathHelper.Clamp(y, 0, map.PixelHeight - viewH);

            // Snap to whole screen pixels. Without this the camera sits on
            // fractions of a pixel and every tile gets rounded on its own, which
            // leaves thin seams along the grid where they don't quite meet.
            // Snapping to 1/Zoom keeps the movement as smooth as the screen can
            // actually show while landing tiles on exact pixels.
            x = MathF.Round(x * GameSettings.Zoom) / GameSettings.Zoom;
            y = MathF.Round(y * GameSettings.Zoom) / GameSettings.Zoom;

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