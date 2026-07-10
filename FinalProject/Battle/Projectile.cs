using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FinalProject.Core;

namespace FinalProject.Battle
{

    public enum ProjectileType
    {
        Normal,
        Smoke
    }
    
    public class Projectile
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public bool IsExpired { get; private set; }

        public ProjectileType Type {get; private set;}

        public int ProjectileWidth => Type switch
        {
            ProjectileType.Smoke => 25,
            _ => GameSettings.DodgeProjectileSize // default size
        };
        
        public int ProjectileHeight => Type switch {
            ProjectileType.Smoke => 25,
            _ => GameSettings.DodgeProjectileSize // default size
        }; 
        
        public Rectangle Bounds => new Rectangle(
            (int)Position.X - ProjectileWidth / 2,
            (int)Position.Y - ProjectileHeight / 2,
            ProjectileWidth,
            ProjectileHeight);

        public Projectile(Vector2 position, Vector2 velocity, ProjectileType type = ProjectileType.Normal) // added defaults
        {
            Position = position;
            Velocity = velocity;
            Type = type;
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
        {
            Color renderColor = Type switch
            {
                ProjectileType.Smoke => Color.Gray * 0.6f,
                _ => Color.OrangeRed
            };
                
            spriteBatch.Draw(pixel, Bounds, renderColor);
        }
    }
}
