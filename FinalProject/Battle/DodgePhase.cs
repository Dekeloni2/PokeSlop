using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FinalProject.Core;
using FinalProject.Core.Input;

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

        private readonly Rectangle   _baseBox;
        private readonly TweeningBox _box;

        private float _elapsed;

        // wait for leftover bullets to clear the arena before ending the turn,
        // otherwise the box starts shrinking back while bullets are still flying
        public bool IsFinished => _elapsed >= _pattern.Duration && _projectiles.Count == 0;
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

            _projectiles.RemoveAll(p => p.IsExpired);
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
        {
            DrawBoxBorder(spriteBatch, pixel);

            foreach (Projectile p in _projectiles)
                p.Draw(spriteBatch, pixel);

            _hitbox.Draw(spriteBatch);
        }

        // ── DodgeContext surface ─────────────────────────────────────────────

        internal void SpawnProjectile(Vector2 position, Vector2 velocity)
            => _projectiles.Add(new Projectile(position, velocity));

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
