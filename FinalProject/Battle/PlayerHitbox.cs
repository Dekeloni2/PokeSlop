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

        // true on frames the player is holding a direction. undertale's blue
        // attacks only hurt you if you're moving (orange is the reverse), so
        // hazards that care read this instead of just testing overlap.
        // based on input, not on actual displacement — holding into a wall still
        // counts as moving, same as the real thing
        public bool IsMoving { get; private set; }

        public Vector2 Position;

        public Rectangle Bounds => new Rectangle(
            (int)Position.X - GameSettings.DodgeHitboxSize / 2,
            (int)Position.Y - GameSettings.DodgeHitboxSize / 2,
            GameSettings.DodgeHitboxSize, GameSettings.DodgeHitboxSize);

        public PlayerHitbox(Vector2 startPosition)
        {
            Position = startPosition;
        }

        // ── grid mode ────────────────────────────────────────────────────────
        // the chess board takes movement over completely — the soul steps one
        // tile at a time instead of moving freely. it commits to the destination
        // the moment you press and then slides there, so which tile it counts as
        // being on is never ambiguous, not even mid-slide
        private const float SlideSeconds = 0.09f;
        private const float StepCooldown = 0.05f; // short beat so steps read as separate

        private float   _stepCooldown;
        private float   _slideDuration = SlideSeconds; // a forced move can be quicker
        private bool    _gridMode;
        private Point   _gridOrigin;  // px, top left of the playable area
        private int     _gridTile;
        private int     _gridCols, _gridRows;
        private Point   _tile;
        private Vector2 _slideFrom;
        private float   _slideLeft;

        public Point Tile => _tile;

        public void EnterGrid(Point originPx, int tileSize, int cols, int rows, Point startTile)
        {
            _gridMode   = true;
            _gridOrigin = originPx;
            _gridTile   = tileSize;
            _gridCols   = cols;
            _gridRows   = rows;
            _tile       = startTile;
            _slideLeft  = 0f;
            Position    = TileCentre(startTile);
        }

        public void ExitGrid() => _gridMode = false;

        // refuses input for a moment without moving anything. the chess board
        // uses it so a knockback reads as landing on the square first and being
        // thrown off it after, rather than never getting there at all
        public void HoldStill(float seconds)
            => _stepCooldown = MathHelper.Max(_stepCooldown, seconds);

        // forced move, for the chess board knocking the soul back off a square
        // it just swung at. slides exactly like a normal step so it doesn't
        // teleport, and the slide finishing applies the usual cooldown
        public void MoveToTile(Point tile, float slideSeconds = SlideSeconds)
        {
            if (!_gridMode) return;

            _slideFrom = Position;
            _tile      = new Point(
                MathHelper.Clamp(tile.X, 0, _gridCols - 1),
                MathHelper.Clamp(tile.Y, 0, _gridRows - 1));

            _slideDuration = MathHelper.Max(slideSeconds, 0.01f);
            _slideLeft     = _slideDuration;

            // a forced move cancels whatever hold was waiting on it. the slides
            // freeze the cooldown rather than tick it, so without this the
            // leftover carries past the bounce and locks you up after landing
            _stepCooldown = 0f;
        }

        public Vector2 TileCentre(Point tile) => new(
            _gridOrigin.X + tile.X * _gridTile + _gridTile / 2f,
            _gridOrigin.Y + tile.Y * _gridTile + _gridTile / 2f);

        // clamps against whatever box is passed in, so a resizing arena just works
        public void Update(GameTime gameTime, InputManager input, Rectangle box)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (_invulnLeft > 0f) _invulnLeft -= dt;

            if (_gridMode) { UpdateGrid(dt, input); return; }

            Vector2 move = Vector2.Zero;
            if (input.IsKeyDown(Keys.Left))  move.X -= 1;
            if (input.IsKeyDown(Keys.Right)) move.X += 1;
            if (input.IsKeyDown(Keys.Up))    move.Y -= 1;
            if (input.IsKeyDown(Keys.Down))  move.Y += 1;

            IsMoving = move != Vector2.Zero;

            if (move != Vector2.Zero)
            {
                move.Normalize();
                Position += move * GameSettings.DodgeHitboxSpeed * dt;
            }

            int half = GameSettings.DodgeHitboxSize / 2;
            Position.X = MathHelper.Clamp(Position.X, box.Left + half, box.Right - half);
            Position.Y = MathHelper.Clamp(Position.Y, box.Top + half, box.Bottom - half);
        }

        // one step at a time — a slide already in progress swallows the input,
        // which doubles as the movement cooldown. holding a direction repeats,
        // otherwise crossing the board would mean eight separate taps
        private void UpdateGrid(float dt, InputManager input)
        {
            if (_slideLeft > 0f)
            {
                _slideLeft -= dt;
                float t = 1f - MathHelper.Clamp(_slideLeft / _slideDuration, 0f, 1f);
                Position = Vector2.Lerp(_slideFrom, TileCentre(_tile), t);
                IsMoving = true;

                if (_slideLeft <= 0f)
                {
                    Position = TileCentre(_tile); // land exactly, no lerp residue
                    // Max, so landing mid-knockback doesn't cut a longer hold short
                    _stepCooldown = MathHelper.Max(_stepCooldown, StepCooldown);
                }
                return;
            }

            if (_stepCooldown > 0f)
            {
                _stepCooldown -= dt;
                IsMoving = false;
                return;
            }

            var step = Point.Zero;
            if      (input.IsKeyDown(Keys.Left))  step.X = -1;
            else if (input.IsKeyDown(Keys.Right)) step.X =  1;
            else if (input.IsKeyDown(Keys.Up))    step.Y = -1;
            else if (input.IsKeyDown(Keys.Down))  step.Y =  1;

            IsMoving = step != Point.Zero;
            if (!IsMoving) return;

            var next = new Point(
                MathHelper.Clamp(_tile.X + step.X, 0, _gridCols - 1),
                MathHelper.Clamp(_tile.Y + step.Y, 0, _gridRows - 1));

            if (next == _tile) return; // already against that edge

            _slideFrom     = Position;
            _tile          = next; // committed up front, collision reads the new tile
            _slideDuration = SlideSeconds;
            _slideLeft     = SlideSeconds;
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
