namespace FinalProject.Entities;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using FinalProject.Core;
using FinalProject.World;
using Sprite = FinalProject.Core.Sprite;

public class Player : Sprite
{
    public Point   TilePosition  { get; private set; }
    public Vector2 WorldPosition { get; private set; }
    public Direction Facing      { get; private set; }
    public bool    IsMoving      { get; private set; }

    private float   _moveTime;
    private float   _moveTimer;
    private Vector2 _moveOrigin;
    private Vector2 _moveDestination;
    private int     _walkStep;

    private readonly Game1 _game;

    // Sprite sheet animation data: 4 directions x 3 frames per direction
    private static readonly int[] FrameX = { 2, 19, 36, 54, 71, 88, 104, 121, 138, 157, 174, 191 };
    private static readonly int[] FrameW = { 15, 15, 16, 14, 14, 14, 14, 14, 14, 14, 14, 14 };
    private const int FrameH = 26;
    private static readonly int[] WalkCycle = { 0, 1, 2, 1 };

    public Player(Game1 game, int startTileX, int startTileY)
        : base(game.Content.Load<Texture2D>("Sprites/Player/player_world"))
    {
        _game         = game;
        TilePosition  = new Point(startTileX, startTileY);
        WorldPosition = TileToWorld(TilePosition);
        Facing        = Direction.Down;
        _moveTime     = GameSettings.TileSize / GameSettings.PlayerSpeed;
    }

    public void Update(GameTime gameTime, TileMap map)
    {
        if (IsMoving)
            UpdateMovement(gameTime);
        else
            HandleInput(map);
    }

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
        _walkStep = (_walkStep + 1) % 4;
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

    public override void Draw(SpriteBatch spriteBatch)
    {
        int animFrame = IsMoving ? WalkCycle[_walkStep % 4] : 1;
        int frameIndex = GetDirectionRow(Facing) + animFrame;

        Rectangle src = new Rectangle(FrameX[frameIndex], 0, FrameW[frameIndex], FrameH);

        int yOffset = -(FrameH - GameSettings.TileSize);
        Vector2 drawPos = new Vector2((int)WorldPosition.X, (int)WorldPosition.Y + yOffset);

        spriteBatch.Draw(Texture, drawPos, src, Color.White);
    }

    private static int GetDirectionRow(Direction d) => d switch
    {
        Direction.Down  => 0,
        Direction.Up    => 3,
        Direction.Left  => 6,
        Direction.Right => 9,
        _               => 0
    };

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
