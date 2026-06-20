// Core/Camera.cs
using Microsoft.Xna.Framework;
using FinalProject.Entities;

namespace FinalProject.Core
{
    // Follows the player and produces a transform matrix that offsets
    // everything drawn so the player stays centered on screen.
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

        // Call this every frame before drawing
        public void Follow(Player player)
        {
            Position = new Vector2(
                player.WorldPosition.X - _halfWidth  + GameSettings.TileSize / 2,
                player.WorldPosition.Y - _halfHeight + GameSettings.TileSize / 2
            );
        }
        
        public Matrix GetTransform()
        {
            return Matrix.CreateTranslation(-Position.X, -Position.Y, 0f);
        }
    }
}