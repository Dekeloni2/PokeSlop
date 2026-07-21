namespace FinalProject.Entities;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using FinalProject.Core;
using FinalProject.Core.Graphics;
using FinalProject.World;

public class Player : Sprite
{
    // free movement, not locked to the grid. WorldPosition is the real position
    // and TilePosition is worked out from it, so anything that still thinks in
    // tiles (warps, interactables, transitions) keeps working
    public Vector2 WorldPosition { get; private set; }
    public Direction Facing      { get; private set; }
    public bool    IsMoving      { get; private set; }

    public Point TilePosition => new Point(
        FloorDiv((int)(WorldPosition.X + _tileSize / 2f), _tileSize),
        FloorDiv((int)(WorldPosition.Y + _tileSize / 2f), _tileSize));

    // he only collides with his lower body, like undertale. the sprite's head
    // and shoulders can overlap walls, which is what makes it feel loose
    private const int FootInsetX = 3;  // px in from each side
    private const float FootHeightFrac = 0.45f; // of a tile, measured up from his feet

    private const float WalkFrameSeconds = 0.12f;

    private float _animTimer;
    private int   _walkStep;

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

    // The student_world spritesheet itself is registered once in
    // Game1.LoadContent via SpriteManager — Player only ever refers to it by
    // name, it doesn't know the content path.
    public Player(Game1 game, int startTileX, int startTileY)
        : base("student_world")
    {
        _game         = game;
        WorldPosition = TileToWorld(new Point(startTileX, startTileY));
        Facing        = Direction.Down;
    }

    public void Update(GameTime gameTime, TileMap map)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        Vector2 input = ReadInput();
        IsMoving = input != Vector2.Zero;

        if (IsMoving)
        {
            FaceAlong(input);

            // each axis is tried on its own so running into a wall diagonally
            // slides along it instead of stopping dead
            Vector2 step = input * GameSettings.PlayerSpeed * dt;
            TryMove(new Vector2(step.X, 0f), map);
            TryMove(new Vector2(0f, step.Y), map);

            _animTimer += dt;
            if (_animTimer >= WalkFrameSeconds)
            {
                _animTimer = 0f;
                _walkStep  = (_walkStep + 1) % 4;
            }
        }
        else
        {
            _animTimer = 0f;
        }
    }

    private Vector2 ReadInput()
    {
        var move = Vector2.Zero;

        if (_game.Input.IsKeyDown(Keys.Left))  move.X -= 1f;
        if (_game.Input.IsKeyDown(Keys.Right)) move.X += 1f;
        if (_game.Input.IsKeyDown(Keys.Up))    move.Y -= 1f;
        if (_game.Input.IsKeyDown(Keys.Down))  move.Y += 1f;

        // so diagonals aren't faster than walking straight
        if (move != Vector2.Zero) move.Normalize();
        return move;
    }

    // horizontal wins when it's clearly the bigger push, otherwise face up/down
    private void FaceAlong(Vector2 move)
    {
        if (System.Math.Abs(move.X) > System.Math.Abs(move.Y))
            Facing = move.X > 0f ? Direction.Right : Direction.Left;
        else if (move.Y != 0f)
            Facing = move.Y > 0f ? Direction.Down : Direction.Up;
    }

    private void TryMove(Vector2 delta, TileMap map)
    {
        if (delta == Vector2.Zero) return;

        Vector2 target = WorldPosition + delta;
        if (IsFootprintClear(target, map)) WorldPosition = target;
    }

    // the box under his feet has to sit entirely on walkable tiles
    private bool IsFootprintClear(Vector2 position, TileMap map)
    {
        int height = (int)(_tileSize * FootHeightFrac);

        int left   = (int)position.X + FootInsetX;
        int right  = (int)position.X + _tileSize - FootInsetX - 1;
        int bottom = (int)position.Y + _tileSize - 1;
        int top    = bottom - height + 1;

        for (int y = FloorDiv(top, _tileSize); y <= FloorDiv(bottom, _tileSize); y++)
            for (int x = FloorDiv(left, _tileSize); x <= FloorDiv(right, _tileSize); x++)
                if (!map.IsWalkable(x, y)) return false;

        return true;
    }

    // normal integer division truncates toward zero, which breaks the tile
    // lookup for anything left of or above the map
    private static int FloorDiv(int value, int divisor)
        => value >= 0 ? value / divisor : (value - divisor + 1) / divisor;

    // turns him without input, for scripted moments like the elevator
    public void Face(Direction direction) => Facing = direction;

    public void Teleport(int tileX, int tileY)
    {
        WorldPosition = TileToWorld(new Point(tileX, tileY));
        IsMoving      = false;
        _animTimer    = 0f;
    }

    // Called by the overworld when a map loads, so world positioning matches
    // that map's tile size. Re-anchors him on the tile he's standing on.
    public void SetTileSize(int tileSize)
    {
        Point tile    = TilePosition; // read before the size changes under it
        _tileSize     = tileSize;
        WorldPosition = TileToWorld(tile);
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
