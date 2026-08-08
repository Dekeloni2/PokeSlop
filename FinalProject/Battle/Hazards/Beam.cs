using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FinalProject.Battle.Hazards
{
    // A persistent hazard (Vegeta's galick gun, Napoleon's beam). Unlike a
    // Projectile it doesn't move or expire on contact — it stays put for its
    // lifetime and deals a flat amount of damage on a fixed cadence while the
    // soul is inside it. DodgePhase owns the list of active beams; patterns
    // add/remove them via DodgeContext.
    public class Beam
    {
        // per-instance rather than shared, so one pattern's beam (garlic
        // gun's) can hit softer than another's (Napoleon's) without either
        // affecting the other. Defaults to the old flat value so anything
        // that doesn't pass one keeps behaving exactly as before.
        public const int DefaultDamage = 5;
        public int Damage { get; }

        private const float DamageInterval = 0.25f; // seconds between damage ticks

        public Rectangle Bounds { get; set; }

        public Texture2D CustomTexture { get; private set; }
        public Color[]   ColorData     { get; private set; }

        private float _damageCooldown; // <= 0 means "ready to hurt again"

        public Beam(Rectangle bounds, int damage = DefaultDamage)
        {
            Bounds = bounds;
            Damage = damage;
        }

        public Beam(Rectangle bounds, Texture2D customTexture, int damage = DefaultDamage)
        {
            Bounds = bounds;
            CustomTexture = customTexture;
            Damage = damage;

            if (customTexture != null)
            {
                ColorData = new Color[customTexture.Width * customTexture.Height];
                customTexture.GetData(ColorData);
            }
        }

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
            if (CustomTexture != null) // for napoleon
            {
                spriteBatch.Draw(CustomTexture, Bounds, Color.White);
                return; // Stop here to skip garlic gun
            }
            
            spriteBatch.Draw(pixel, Bounds, new Color(128, 0, 128)); // galick-gun purple

            // brighter core line down the middle for a beam-y look
            int coreH = Math.Max(2, Bounds.Height / 3);
            var core = new Rectangle(
                Bounds.X, Bounds.Y + (Bounds.Height - coreH) / 2, Bounds.Width, coreH);
            spriteBatch.Draw(pixel, core, new Color(190, 225, 255));
        }
    }
}
