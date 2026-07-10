using Microsoft.Xna.Framework;
using FinalProject.Core;

namespace FinalProject.Battle.Patterns
{
    // test pattern - expands the arena, fires shots aimed at the player,
    // shrinks back near the end. Mostly here to prove arena resizing works.
    public class ExpandingBoxPattern : IBulletPattern
    {
        public float Duration => 8f;

        private const float ResizeTime = 1.5f;

        private float _spawnTimer;
        private bool  _shrinkBackStarted;

        public void Start(DodgeContext context)
        {
            Rectangle expanded = new Rectangle(
                context.BaseBox.X - 40, context.BaseBox.Y - 40,
                context.BaseBox.Width + 80, context.BaseBox.Height + 80);

            context.ResizeBoxTo(expanded, ResizeTime);
        }

        public void Update(GameTime gameTime, DodgeContext context)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // shrink back before the turn ends (flag so this only fires once)
            if (!_shrinkBackStarted && context.Elapsed >= Duration - ResizeTime)
            {
                context.ResizeBoxTo(context.BaseBox, ResizeTime);
                _shrinkBackStarted = true;
            }

            // stop spawning once time is up, leftover bullets still need to
            // clear the arena before the turn ends (see DodgePhase.IsFinished)
            if (context.Elapsed >= Duration) return;

            _spawnTimer += dt;
            if (_spawnTimer < GameSettings.DodgeSpawnInterval * 2f) return;
            _spawnTimer = 0f;

            Rectangle box = context.CurrentBox;
            Vector2 spawnPos = new Vector2(box.Center.X, box.Top);

            Vector2 dir = context.HitboxPosition - spawnPos;
            if (dir == Vector2.Zero) dir = new Vector2(0f, 1f);
            dir.Normalize();

            context.SpawnProjectile(spawnPos, dir * GameSettings.DodgeProjectileSpeed);
        }
    }
}
