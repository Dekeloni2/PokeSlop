using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FinalProject.Battle
{
    // Owns a pool of decorative particles and drives them. Whoever runs a scene
    // keeps one of these — DodgePhase for attack effects, and later BattleState
    // for the teacher's death dust. Deliberately not a global/static manager, so
    // each owner draws its particles in its own coordinate space.
    public class ParticleSystem
    {
        private readonly List<Particle> _particles = new();

        public int Count => _particles.Count;

        public void Spawn(Vector2 position, Vector2 velocity, float lifeSeconds,
                          int size, Color color, float fadeInSeconds = 0.3f,
                          Texture2D texture = null)
            => _particles.Add(new Particle(position, velocity, lifeSeconds, size, color,
                                           fadeInSeconds, texture));

        public void Update(float dt)
        {
            foreach (Particle p in _particles)
                p.Update(dt);

            _particles.RemoveAll(p => p.IsFinished);
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
        {
            foreach (Particle p in _particles)
                p.Draw(spriteBatch, pixel);
        }

        public void Clear() => _particles.Clear();
    }
}
