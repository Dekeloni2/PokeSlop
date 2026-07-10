using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FinalProject.Core;

namespace FinalProject.Battle
{
    public class Projectile
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public bool IsExpired { get; private set; }

        public Rectangle Bounds => new Rectangle(
            (int)Position.X - GameSettings.DodgeProjectileSize / 2,
            (int)Position.Y - GameSettings.DodgeProjectileSize / 2,
            GameSettings.DodgeProjectileSize, GameSettings.DodgeProjectileSize);

        public Projectile(Vector2 position, Vector2 velocity)
        {
            Position = position;
            Velocity = velocity;
        }

        // expires once it's fully outside the current box (plus a margin)
        public void Update(GameTime gameTime, Rectangle box)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            Position += Velocity * dt;

            Rectangle expireBounds = new Rectangle(
                box.X - GameSettings.DodgeBoundsMargin,
                box.Y - GameSettings.DodgeBoundsMargin,
                box.Width  + GameSettings.DodgeBoundsMargin * 2,
                box.Height + GameSettings.DodgeBoundsMargin * 2);

            if (!expireBounds.Contains(Position.ToPoint()))
                IsExpired = true;
        }

        // called on hit so the same bullet can't hit twice
        public void Expire() => IsExpired = true;

        public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
            => spriteBatch.Draw(pixel, Bounds, Color.OrangeRed);
    }
}
