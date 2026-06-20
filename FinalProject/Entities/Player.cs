namespace FinalProject.Entities;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using FinalProject.Core;
using FinalProject.World;

    public class Player
    {
        public Point     TilePosition  { get; private set; }
        public Vector2   WorldPosition { get; private set; }
        public Direction Facing        { get; private set; }
        public bool      IsMoving      { get; private set; }

        // How long (in seconds) it takes to slide one tile
        private readonly float _moveTime;

        private float   _moveTimer;
        private Vector2 _moveOrigin;
        private Vector2 _moveDestination;

        private readonly Game1 _game;

        public Player(Game1 game, int startTileX, int startTileY)
        {
            _game        = game;
            TilePosition = new Point(startTileX, startTileY);
            WorldPosition = TileToWorld(TilePosition);
            Facing       = Direction.Down;

            // Derive move time from speed constant so one value controls both
            _moveTime = GameSettings.TileSize / GameSettings.PlayerSpeed;
        }

        // map is used for collision and bounds checks
        public void Update(GameTime gameTime, TileMap map)
        {
            if (IsMoving)
                UpdateMovement(gameTime);
            else
                HandleInput(map);
        }

        private void HandleInput(TileMap map)
        {
            Direction? input = null;

            if      (_game.Input.IsKeyDown(Keys.Up))    input = Direction.Up;
            else if (_game.Input.IsKeyDown(Keys.Down))  input = Direction.Down;
            else if (_game.Input.IsKeyDown(Keys.Left))  input = Direction.Left;
            else if (_game.Input.IsKeyDown(Keys.Right)) input = Direction.Right;

            if (input == null) return;

            Facing = input.Value;

            Point next = GetNeighbour(TilePosition, input.Value);

            // IsWalkable returns false for out-of-bounds AND for Object-layer tiles
            if (!map.IsWalkable(next.X, next.Y)) return;

            StartMoving(next);
        }

        private void StartMoving(Point destination)
        {
            IsMoving         = true;
            _moveTimer       = 0f;
            _moveOrigin      = WorldPosition;
            _moveDestination = TileToWorld(destination);

            // Update logical position immediately —
            // collision and encounter checks always use TilePosition
            TilePosition = destination;
        }

        private void UpdateMovement(GameTime gameTime)
        {
            _moveTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;

            float t = MathHelper.Clamp(_moveTimer / _moveTime, 0f, 1f);
            WorldPosition = Vector2.Lerp(_moveOrigin, _moveDestination, t);

            if (t >= 1f)
            {
                WorldPosition = _moveDestination;
                IsMoving      = false;
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            // Placeholder rectangle until sprites are loaded
            Rectangle rect = new Rectangle(
                (int)WorldPosition.X,
                (int)WorldPosition.Y,
                GameSettings.TileSize,
                GameSettings.TileSize
            );

            spriteBatch.Draw(_game.PixelTexture, rect, Color.Red);
        }

        // Converts a tile coordinate to its top-left pixel position in the world
        private static Vector2 TileToWorld(Point tile)
            => new Vector2(tile.X * GameSettings.TileSize, tile.Y * GameSettings.TileSize);

        // Returns the tile adjacent to a given tile in a given direction
        private static Point GetNeighbour(Point from, Direction direction)
        {
            return direction switch
            {
                Direction.Up    => new Point(from.X,     from.Y - 1),
                Direction.Down  => new Point(from.X,     from.Y + 1),
                Direction.Left  => new Point(from.X - 1, from.Y),
                Direction.Right => new Point(from.X + 1, from.Y),
                _               => from
            };
        }
    }