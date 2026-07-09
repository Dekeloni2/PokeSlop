using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using FinalProject.Core;
using FinalProject.Core.Graphics;
using FinalProject.Core.Input;

namespace FinalProject.Battle
{
    // The FIGHT timing bar. A bar sweeps across the target image, Z stops it,
    // and the closer it stops to the center the more damage you deal. If the
    // bar leaves the box it's a miss - no damage but the turn is still spent.
    public class AttackMinigame
    {
        private enum State { Sweeping, Flashing, Done }

        private readonly Rectangle _zone; // box interior, same size as the target art
        private readonly int _direction;  // 1 = left to right, -1 = right to left

        private State _state = State.Sweeping;
        private float _barX;
        private float _flashElapsed;

        public bool IsFinished => _state == State.Done;
        public bool Missed { get; private set; }

        // 1 at the center, 0 at the edges
        public float DamageMultiplier { get; private set; }

        public AttackMinigame(Rectangle zone, Random rng)
        {
            _zone      = zone;
            _direction = rng.Next(2) == 0 ? 1 : -1;
            _barX      = _direction == 1 ? zone.Left : zone.Right;
        }

        public void Update(GameTime gameTime, InputManager input)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            switch (_state)
            {
                case State.Sweeping:
                    _barX += _direction * GameSettings.AttackBarSpeed * dt;

                    if (_barX < _zone.Left || _barX > _zone.Right)
                    {
                        Missed = true;
                        _state = State.Done;
                        break;
                    }

                    if (input.IsKeyPressed(Keys.Z))
                    {
                        float halfWidth = _zone.Width / 2f;
                        DamageMultiplier = 1f - Math.Abs(_barX - _zone.Center.X) / halfWidth;
                        _state = State.Flashing;
                    }
                    break;

                case State.Flashing:
                    _flashElapsed += dt;
                    if (_flashElapsed >= GameSettings.AttackFlashSeconds)
                        _state = State.Done;
                    break;
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            Spritesheet zone = SpriteManager.GetSprite("attackZone");
            spriteBatch.Draw(zone.Texture, _zone, zone[0, 0], Color.White);

            if (Missed) return;

            // frame 0 = white bar, frame 1 = inverted one. They alternate while flashing
            Spritesheet bar = SpriteManager.GetSprite("attackBar");
            int frame = _state == State.Flashing
                && (int)(_flashElapsed / GameSettings.AttackFlashInterval) % 2 == 1 ? 1 : 0;

            Rectangle src = bar[frame, 0];
            var dest = new Rectangle(
                (int)(_barX - src.Width / 2f),
                _zone.Center.Y - src.Height / 2,
                src.Width, src.Height);

            spriteBatch.Draw(bar.Texture, dest, src, Color.White);
        }
    }
}
