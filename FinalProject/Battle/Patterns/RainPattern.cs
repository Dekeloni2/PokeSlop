using System;
using Microsoft.Xna.Framework;
using FinalProject.Core;

namespace FinalProject.Battle.Patterns
{
    // test pattern - bullets fall straight down from random spots along the top.
    // TODO replace with real teacher attacks
    public class RainPattern : IBulletPattern
    {
        public float Duration => 6f;

        private readonly Random _rng = new();
        private float _spawnTimer;

        public void Start(DodgeContext context) { }

        public void Update(GameTime gameTime, DodgeContext context)
        {
            // stop spawning once time is up, leftover bullets still need to
            // clear the arena before the turn ends (see DodgePhase.IsFinished)
            if (context.Elapsed >= Duration) return;

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _spawnTimer += dt;

            if (_spawnTimer < GameSettings.DodgeSpawnInterval) return;
            _spawnTimer = 0f;

            Rectangle box = context.CurrentBox;
            float x = box.Left + (float)_rng.NextDouble() * box.Width;

            context.SpawnProjectile(
                new Vector2(x, box.Top),
                new Vector2(0f, GameSettings.DodgeProjectileSpeed));
        }
    }
}
