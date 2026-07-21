namespace FinalProject.Entities;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using FinalProject.Core;
using FinalProject.Core.Graphics;
using FinalProject.World;

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

    // pixel size of a tile on the CURRENT map. Maps can differ (16px vs 20px),
    // so this follows the loaded map rather than a global constant — otherwise
    // the player is positioned on a different pixel grid than the map is drawn on.
    private int     _tileSize = GameSettings.TileSize;

    private readonly Game1 _game;
    
    private static readonly Rectangle[] DownFrames =
    {
        new Rectangle(3,  3, 19, 30),
        new Rectangle(26, 3, 19, 30),
        new Rectangle(49, 3, 19, 30),
        new Rectangle(72, 3, 19, 30),
    };

    private static readonly Rectangle[] LeftFrames =
    {
        new Rectangle(3,  37, 19, 29),
        new Rectangle(26, 37, 19, 29),
        new Rectangle(3,  37, 19, 29),
        new Rectangle(26, 37, 19, 29),
    };

    private static readonly Rectangle[] UpFrames =
    {
        new Rectangle(3,  69, 19, 30),
        new Rectangle(26, 69, 19, 30),
        new Rectangle(49, 69, 19, 30),
        new Rectangle(72, 69, 19, 30),
    };

    // Which frame to show while standing still — index into whichever row is
    // active. Tweak this if a different pose reads better once in-game.
    private const int IdleFrame = 0;

    // Maps keyboard keys to movement directions — add/remap bindings here
    private static readonly (Keys Key, Direction Dir)[] _keyBindings =
    {
        (Keys.Up,    Direction.Up),
        (Keys.Down,  Direction.Down),
        (Keys.Left,  Direction.Left),
        (Keys.Right, Direction.Right),
    };

    // The student_world spritesheet itself is registered once in
    // Game1.LoadContent via SpriteManager — Player only ever refers to it by
    // name, it doesn't know the content path.
    public Player(Game1 game, int startTileX, int startTileY)
        : base("student_world")
    {
        _game         = game;
        TilePosition  = new Point(startTileX, startTileY);
        WorldPosition = TileToWorld(TilePosition);
        Facing        = Direction.Down;
        _moveTime     = _tileSize / GameSettings.PlayerSpeed;
    }

    public void Update(GameTime gameTime, TileMap map)
    {
        if (IsMoving)
            UpdateMovement(gameTime);
        else
            HandleInput(map);
    }

    // turns him without input, for scripted moments like the elevator
    public void Face(Direction direction) => Facing = direction;

    public void Teleport(int tileX, int tileY)
    {
        TilePosition  = new Point(tileX, tileY);
        WorldPosition = TileToWorld(TilePosition);
        IsMoving      = false;
        _moveTimer    = 0f;
    }

    // Called by the overworld when a map loads, so world positioning and
    // step timing match that map's tile size. Re-anchors the current tile.
    public void SetTileSize(int tileSize)
    {
        _tileSize     = tileSize;
        _moveTime     = _tileSize / GameSettings.PlayerSpeed;
        WorldPosition = TileToWorld(TilePosition);
    }

    private void HandleInput(TileMap map)
    {
        Direction? input = null;

        // Prefer a freshly pressed key over one already held — this way the most
        // recently pressed direction always wins when two keys are held at once,
        // preventing the facing/movement mismatch bug.
        foreach (var (key, direction) in _keyBindings)
            if (_game.Input.IsKeyPressed(key)) { input = direction; break; }

        if (input == null)
            foreach (var (key, direction) in _keyBindings)
                if (_game.Input.IsKeyDown(key)) { input = direction; break; }

        if (input == null) return;

        Facing = input.Value;

        Point next = input.Value.GetNeighbour(TilePosition);
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
        float lerpProgress = MathHelper.Clamp(_moveTimer / _moveTime, 0f, 1f);
        WorldPosition = Vector2.Lerp(_moveOrigin, _moveDestination, lerpProgress);

        if (lerpProgress >= 1f)
        {
            WorldPosition = _moveDestination;
            IsMoving      = false;
        }
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        // _walkStep already cycles 0-3 (one full lap per 4 completed steps),
        // matching the 4 real frames per row — no remapping needed.
        int frameIndex = IsMoving ? _walkStep : IdleFrame;

        (Rectangle[] frames, bool flip) = GetFrameSet(Facing);
        Rectangle src = frames[frameIndex];

        int yOffset = -(src.Height - _tileSize);
        Vector2 drawPos = new Vector2((int)WorldPosition.X, (int)WorldPosition.Y + yOffset);

        SpriteEffects effects = flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        spriteBatch.Draw(Texture, drawPos, src, Color.White, 0f, Vector2.Zero, 1f, effects, 0f);
    }

    // Draws the current frame in SCREEN space, centered on screenCenter and
    // scaled — used by the battle intro to show the frozen player on black.
    public void DrawFrozen(SpriteBatch spriteBatch, Vector2 screenCenter, float scale)
    {
        int frameIndex = IsMoving ? _walkStep : IdleFrame;
        (Rectangle[] frames, bool flip) = GetFrameSet(Facing);
        Rectangle src = frames[frameIndex];

        int w = (int)(src.Width  * scale);
        int h = (int)(src.Height * scale);
        var dst = new Rectangle((int)screenCenter.X - w / 2, (int)screenCenter.Y - h / 2, w, h);

        SpriteEffects effects = flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        spriteBatch.Draw(Texture, dst, src, Color.White, 0f, Vector2.Zero, effects, 0f);
    }

    // Right isn't drawn from the sheet — it's the Left row flipped horizontally.
    private static (Rectangle[] Frames, bool Flip) GetFrameSet(Direction facing) => facing switch
    {
        Direction.Down  => (DownFrames, false),
        Direction.Up    => (UpFrames,   false),
        Direction.Left  => (LeftFrames, false),
        Direction.Right => (LeftFrames, true),
        _               => (DownFrames, false)
    };

    private Vector2 TileToWorld(Point tile)
        => new Vector2(tile.X * _tileSize, tile.Y * _tileSize);

}
