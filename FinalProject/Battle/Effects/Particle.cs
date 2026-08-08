using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FinalProject.Battle.Effects
{
    // small visual speck. unlike Projectile/Beam/HexHazard it never damages the
    // player, it just drifts and fades out. fades in at the start of its life
    // and back out at the end
    public class Particle
    {
        private readonly float     _maxLife;
        private readonly float     _fadeIn;
        private readonly int       _size;
        private readonly Color     _color;
        private readonly Texture2D _texture; // null = plain square from the 1x1 pixel texture

        private Vector2 _position;
        private Vector2 _velocity;
        private float   _life;

        public bool IsFinished => _life <= 0f;

        public Particle(Vector2 position, Vector2 velocity, float lifeSeconds,
                        int size, Color color, float fadeInSeconds = 0.3f,
                        Texture2D texture = null)
        {
            _position = position;
            _velocity = velocity;
            _maxLife  = lifeSeconds;
            _life     = lifeSeconds;
            _size     = size;
            _color    = color;
            _fadeIn   = fadeInSeconds;
            _texture  = texture;
        }

        public void Update(float dt)
        {
            _position += _velocity * dt;
            _life     -= dt;
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
        {
            // fade up over _fadeIn, then back down over the last 30% of its life
            float elapsed = _maxLife - _life;
            float alpha   = _fadeIn > 0f ? MathHelper.Clamp(elapsed / _fadeIn, 0f, 1f) : 1f;

            float tail = _maxLife * 0.3f;
            if (tail > 0f && _life < tail)
                alpha *= MathHelper.Clamp(_life / tail, 0f, 1f);

            spriteBatch.Draw(_texture ?? pixel,
                new Rectangle((int)_position.X, (int)_position.Y, _size, _size),
                _color * alpha);
        }
    }
}
