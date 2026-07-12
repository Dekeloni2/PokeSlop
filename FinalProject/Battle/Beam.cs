using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FinalProject.Battle
{
    // A persistent hazard (Vegeta's galick gun). Unlike a Projectile it doesn't
    // move or expire on contact — it stays put for its lifetime and deals a flat
    // 1 damage on a fixed cadence while the soul is inside it. DodgePhase owns
    // the list of active beams; patterns add/remove them via DodgeContext.
    public class Beam
    {
        public const int   Damage         = 1;
        private const float DamageInterval = 0.25f; // seconds between damage ticks

        public Rectangle Bounds { get; set; }

        private float _damageCooldown; // <= 0 means "ready to hurt again"

        public Beam(Rectangle bounds) => Bounds = bounds;

        // Ticks the damage cadence. Returns true on the frames the player should
        // take a hit — hits immediately on entry, then once per DamageInterval
        // while still inside.
        public bool Tick(float dt, Rectangle hitbox)
        {
            _damageCooldown -= dt;

            if (!Bounds.Intersects(hitbox) || _damageCooldown > 0f)
                return false;

            _damageCooldown = DamageInterval;
            return true;
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
        {
            spriteBatch.Draw(pixel, Bounds, new Color(40, 120, 255)); // galick-gun blue

            // brighter core line down the middle for a beam-y look
            int coreH = Math.Max(2, Bounds.Height / 3);
            var core = new Rectangle(
                Bounds.X, Bounds.Y + (Bounds.Height - coreH) / 2, Bounds.Width, coreH);
            spriteBatch.Draw(pixel, core, new Color(190, 225, 255));
        }
    }
}
