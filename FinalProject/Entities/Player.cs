namespace FinalProject.Entities;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using FinalProject.Core;
using FinalProject.World;

public class Player
{
    public Point   TilePosition  { get; private set; }
    public Vector2 WorldPosition { get; private set; }
    public Direction Facing      { get; private set; }
    public bool    IsMoving      { get; private set; }

    private float   _moveTime;
    private float   _moveTimer;
    private Vector2 _moveOrigin;
    private Vector2 _moveDestination;

    private readonly Game1     _game;
    private readonly Texture2D _texture;

    // Sprite sheet has 12 frames in a single row: 4 directions x 3 walk frames
    // Each frame is ~16px wide, 26px tall.
    // Detected x-positions and widths for each frame (widths vary slightly across the sheet)
    private static readonly int[] FrameX = { 2, 19, 36, 54, 71, 88, 104, 121, 138, 157, 174, 191 };
    private static readonly int[] FrameW = { 15, 15, 16, 14, 14, 14, 14, 14, 14, 14, 14, 14 };
    private const int FrameH = 26;

    // Direction → starting frame index in the sheet (groups of 3)
    private static int DirectionRow(Direction d) => d switch
    {
        Direction.Down  => 0,
        Direction.Up    => 3,
        Direction.Left  => 6,
        Direction.Right => 9,
        _               => 0
    };

    // Cycles through the 4-frame walk animation: left foot, standing, right foot, standing
    private int _walkStep;
    private static readonly int[] WalkCycle = { 0, 1, 2, 1 };

    public Player(Game1 game, int startTileX, int startTileY)
    {
        _game         = game;
        _texture      = game.Content.Load<Texture2D>("Sprites/Player/player_world");
        TilePosition  = new Point(startTileX, startTileY);
        WorldPosition = TileToWorld(TilePosition);
        Facing        = Direction.Down;
        _moveTime     = GameSettings.TileSize / GameSettings.PlayerSpeed;
    }

    // map is used for collision and bounds checks
    public void Update(GameTime gameTime, TileMap map)
    {
        if (IsMoving)
            UpdateMovement(gameTime);
        else
            HandleInput(map);
    }

    // Instantly places the player on a tile — used on map transitions
    public void Teleport(int tileX, int tileY)
    {
        TilePosition  = new Point(tileX, tileY);
        WorldPosition = TileToWorld(TilePosition);
        IsMoving      = false;
        _moveTimer    = 0f;
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
        if (!map.IsWalkable(next.X, next.Y)) return;

        StartMoving(next);
    }

    private void StartMoving(Point destination)
    {
        IsMoving         = true;
        _moveTimer       = 0f;
        _moveOrigin      = WorldPosition;
        _moveDestination = TileToWorld(destination);
        TilePosition     = destination;
        _walkStep = (_walkStep + 1) % 4;  // advance through left, stand, right, stand
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
        // Pick the walk frame: idle = 1, left foot = 0, right foot = 2
        int animFrame = IsMoving ? WalkCycle[_walkStep] : 1;
        int frameIndex = DirectionRow(Facing) + animFrame;

        Rectangle src = new Rectangle(FrameX[frameIndex], 0, FrameW[frameIndex], FrameH);

        // Draw sprite with feet aligned to the tile bottom
        // (sprite is taller than 1 tile so shift it up by the overflow)
        int yOffset = -(FrameH - GameSettings.TileSize);
        Vector2 drawPos = new Vector2((int)WorldPosition.X, (int)WorldPosition.Y + yOffset);

        spriteBatch.Draw(_texture, drawPos, src, Color.White);
    }

    private static Vector2 TileToWorld(Point tile)
        => new Vector2(tile.X * GameSettings.TileSize, tile.Y * GameSettings.TileSize);

    private static Point GetNeighbour(Point from, Direction direction) => direction switch
    {
        Direction.Up    => new Point(from.X,     from.Y - 1),
        Direction.Down  => new Point(from.X,     from.Y + 1),
        Direction.Left  => new Point(from.X - 1, from.Y),
        Direction.Right => new Point(from.X + 1, from.Y),
        _               => from
    };
}
