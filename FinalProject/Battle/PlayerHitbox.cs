using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using FinalProject.Core;
using FinalProject.Core.Graphics;
using FinalProject.Core.Input;

namespace FinalProject.Battle
{
    public class PlayerHitbox
    {
        // after a hit the soul flashes and can't be hurt again for a moment,
        // same as undertale. DodgePhase checks IsInvulnerable before damaging
        private const float InvulnSeconds = 1.0f;
        private const float FlashInterval = 0.07f;

        private float _invulnLeft;

        public bool IsInvulnerable => _invulnLeft > 0f;

        public void TakeHit() => _invulnLeft = InvulnSeconds;

        public Vector2 Position;

        public Rectangle Bounds => new Rectangle(
            (int)Position.X - GameSettings.DodgeHitboxSize / 2,
            (int)Position.Y - GameSettings.DodgeHitboxSize / 2,
            GameSettings.DodgeHitboxSize, GameSettings.DodgeHitboxSize);

        public PlayerHitbox(Vector2 startPosition)
        {
            Position = startPosition;
        }

        // clamps against whatever box is passed in, so a resizing arena just works
        public void Update(GameTime gameTime, InputManager input, Rectangle box)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (_invulnLeft > 0f) _invulnLeft -= dt;

            Vector2 move = Vector2.Zero;
            if (input.IsKeyDown(Keys.Left))  move.X -= 1;
            if (input.IsKeyDown(Keys.Right)) move.X += 1;
            if (input.IsKeyDown(Keys.Up))    move.Y -= 1;
            if (input.IsKeyDown(Keys.Down))  move.Y += 1;

            if (move != Vector2.Zero)
            {
                move.Normalize();
                Position += move * GameSettings.DodgeHitboxSpeed * dt;
            }

            int half = GameSettings.DodgeHitboxSize / 2;
            Position.X = MathHelper.Clamp(Position.X, box.Left + half, box.Right - half);
            Position.Y = MathHelper.Clamp(Position.Y, box.Top + half, box.Bottom - half);
        }

        // the sprite is drawn bigger than the actual hitbox (like undertale,
        // the visible heart is more forgiving than it looks)
        public void Draw(SpriteBatch spriteBatch)
        {
            Spritesheet soul = SpriteManager.GetSprite("soul");

            // flashes between the normal and the darker soul while invulnerable
            bool dark = IsInvulnerable && (int)(_invulnLeft / FlashInterval) % 2 == 1;
            Rectangle src = soul[dark ? 1 : 0, 0];

            var dest = new Rectangle(
                (int)Position.X - src.Width / 2, (int)Position.Y - src.Height / 2,
                src.Width, src.Height);
            spriteBatch.Draw(soul.Texture, dest, src, Color.White);
        }
    }
}
