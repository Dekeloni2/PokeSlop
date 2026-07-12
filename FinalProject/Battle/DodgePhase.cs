using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FinalProject.Core;
using FinalProject.Core.Input;
using FinalProject.Core.Graphics;
using FinalProject.Battle.Patterns;

namespace FinalProject.Battle
{
    // runs one enemy attack: updates the bullet pattern, moves the soul,
    // handles collisions and damage
    public class DodgePhase
    {
        private readonly IBulletPattern _pattern;
        private readonly Teacher        _teacher;
        private readonly PlayerData     _playerData;
        private readonly PlayerHitbox   _hitbox;
        private readonly List<Projectile> _projectiles = new();
        private readonly List<Beam>       _beams       = new();

        private readonly Rectangle   _baseBox;
        private readonly TweeningBox _box;

        private float _elapsed;

        // wait for leftover bullets/beams to clear the arena before ending the
        // turn, otherwise the box starts shrinking back while hazards are live
        public bool IsFinished => _elapsed >= _pattern.Duration
                                  && _projectiles.Count == 0 && _beams.Count == 0;
        public Rectangle CurrentBox => _box.Current;

        internal float     Elapsed        => _elapsed;
        internal Rectangle BaseBox        => _baseBox;
        internal Vector2   HitboxPosition => _hitbox.Position;

        public DodgePhase(IBulletPattern pattern, Teacher teacher, PlayerData playerData, Rectangle baseBox)
        {
            _pattern    = pattern;
            _teacher    = teacher;
            _playerData = playerData;
            _baseBox    = baseBox;
            _box        = new TweeningBox(baseBox);
            _hitbox     = new PlayerHitbox(new Vector2(baseBox.Center.X, baseBox.Center.Y));

            _pattern.Start(new DodgeContext(this));
        }

        public void Update(GameTime gameTime, InputManager input)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _elapsed += dt;

            _box.Update(dt);

            _pattern.Update(gameTime, new DodgeContext(this));

            _hitbox.Update(gameTime, input, _box.Current);

            foreach (Projectile p in _projectiles)
                p.Update(gameTime, _box.Current);

            CheckCollisions();
            CheckBeamDamage(dt);

            _projectiles.RemoveAll(p => p.IsExpired);
        }

        
        public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
        {
            if (_pattern is BoatPattern)
            {
                Spritesheet boat = SpriteManager.GetSprite("boat");

                Rectangle boatRect = new Rectangle(
                    CurrentBox.Center.X - boat.Texture.Width * 2 / 2,
                    CurrentBox.Bottom + 5,
                    boat.Texture.Width * 2,
                    boat.Texture.Height );

                spriteBatch.Draw(boat.Texture, boatRect, boat[0, 0], Color.White);
            }
            
            if (_pattern is GarlicGunPattern garlic && (garlic.IsCharging || garlic.IsFiring || garlic.IsVanishing))
            {
                Rectangle laneRect = garlic.BeamStrip(CurrentBox);

                // tile several "!" markers across the lane instead of stretching
                // one, so it reads as a clear "danger here" strip, not a smear
                if (garlic.IsCharging)
                {
                    Spritesheet warning = SpriteManager.GetSprite("warning");
                    if (warning != null)
                    {
                        Rectangle frame = warning[garlic.WarningFrame, 0];

                        // draw each marker at its native pixel size (1:1) so point
                        // sampling stays crisp — scaling to a non-matching size is
                        // what made them look warped. Space them evenly along the lane.
                        int markerW = frame.Width;
                        int markerH = frame.Height;
                        int stride  = markerW + markerW / 3; // native width + a gap
                        int count   = laneRect.Width / stride;
                        if (count < 1) count = 1;
                        float step = laneRect.Width / (float)count;
                        int y = laneRect.Center.Y - markerH / 2;

                        // reveal the markers one at a time across the charge, from
                        // Vegeta's side outward (the way the beam will travel)
                        int shown = (int)(count * garlic.ChargeProgress) + 1;
                        if (shown > count) shown = count;

                        for (int i = 0; i < count; i++)
                        {
                            bool revealed = garlic.FromLeft ? i < shown : i >= count - shown;
                            if (!revealed) continue;

                            int cx = laneRect.Left + (int)(i * step + (step - markerW) / 2f);
                            spriteBatch.Draw(warning.Texture,
                                new Rectangle(cx, y, markerW, markerH), frame, Color.White);
                        }
                    }
                }

                // Vegeta stands just off the firing side, facing inward
                Spritesheet vegeta = SpriteManager.GetSprite("vegeta");
                if (vegeta != null)
                {
                    Rectangle src = vegeta[garlic.VegetaFrame % vegeta.Columns, 0];
                    int w = src.Width * 2, h = src.Height * 2;
                    int vy = laneRect.Center.Y - h / 2;
                    Rectangle dst = garlic.FromLeft
                        ? new Rectangle(CurrentBox.Left - w, vy, w, h)
                        : new Rectangle(CurrentBox.Right, vy, w, h);
                    SpriteEffects fx = garlic.FromLeft ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
                    spriteBatch.Draw(vegeta.Texture, dst, src, Color.White, 0f, Vector2.Zero, fx, 0f);
                }
            }

            DrawBoxBorder(spriteBatch, pixel);

            foreach (Beam beam in _beams)
                beam.Draw(spriteBatch, pixel);

            foreach (Projectile p in _projectiles)
                p.Draw(spriteBatch, pixel);

            _hitbox.Draw(spriteBatch);
            
            
        }

        // ── DodgeContext surface ─────────────────────────────────────────────

        internal void SpawnProjectile(Vector2 position, Vector2 velocity, ProjectileType type = ProjectileType.Normal)
            => _projectiles.Add(new Projectile(position, velocity,  type));

        internal Beam AddBeam(Rectangle bounds)
        {
            var beam = new Beam(bounds);
            _beams.Add(beam);
            return beam;
        }

        internal void RemoveBeam(Beam beam) => _beams.Remove(beam);

        internal void ResizeBoxTo(Rectangle target, float overSeconds)
            => _box.ResizeTo(target, overSeconds);

        // ── Helpers ──────────────────────────────────────────────────────────

        private void CheckCollisions()
        {
            foreach (Projectile p in _projectiles)
            {
                if (p.IsExpired) continue;

                if (p.Bounds.Intersects(_hitbox.Bounds))
                {
                    _playerData.TakeDamage(_teacher.Attack);
                    p.Expire(); // so the same bullet can't hit twice
                }
            }
        }

        // beams don't expire on contact — they deal a flat 1 damage on a fixed
        // cadence for as long as the soul stays inside them
        private void CheckBeamDamage(float dt)
        {
            foreach (Beam beam in _beams)
                if (beam.Tick(dt, _hitbox.Bounds))
                    _playerData.TakeDamage(Beam.Damage);
        }

        private void DrawBoxBorder(SpriteBatch spriteBatch, Texture2D pixel)
        {
            const int borderThickness = 2;
            Rectangle boxRect = _box.Current;

            spriteBatch.Draw(pixel, new Rectangle(boxRect.X, boxRect.Y, boxRect.Width, borderThickness), Color.White);
            spriteBatch.Draw(pixel, new Rectangle(boxRect.X, boxRect.Bottom - borderThickness, boxRect.Width, borderThickness), Color.White);
            spriteBatch.Draw(pixel, new Rectangle(boxRect.X, boxRect.Y, borderThickness, boxRect.Height), Color.White);
            spriteBatch.Draw(pixel, new Rectangle(boxRect.Right - borderThickness, boxRect.Y, borderThickness, boxRect.Height), Color.White);
        }
    }
}
