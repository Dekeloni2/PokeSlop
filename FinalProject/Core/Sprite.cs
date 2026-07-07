namespace FinalProject.Core;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

// A drawable backed by a named spritesheet (see SpriteManager) plus a
// Transform for position/rotation/scale. Subclasses that need custom framing
// (e.g. Player, which picks a frame by facing direction rather than cycling
// the whole sheet) can ignore Transform/SourceRect and override Draw
// entirely — see Animation for a subclass that plays the sheet as one
// linear, looping animation instead.
public class Sprite : IGameDrawable
{
    public Transform     Transform    { get; } = new Transform();
    public Spritesheet   Spritesheet  { get; }
    public Color         Color        { get; set; } = Color.White;
    public int           SortingOrder { get; set; } = 0;
    public SpriteEffects Effects      { get; set; } = SpriteEffects.None;

    protected Texture2D  Texture { get; set; }
    protected Rectangle? SourceRect;
    protected Rectangle  DestRect;

    private Vector2 _origin = Vector2.Zero;

    public Sprite(string spriteName)
    {
        Spritesheet = SpriteManager.GetSprite(spriteName);
        Texture     = Spritesheet.Texture;
    }

    public virtual void Start()
    {
        SourceRect = Texture.Bounds;
    }

    public virtual void Update(GameTime gameTime)
    {
        // Origin must be recalculated after the source rect updates, which
        // happens in Animation.Update — so this needs to run after that, not before.
        _origin = new Vector2(SourceRect.Value.Width * 0.5f, SourceRect.Value.Height * 0.5f);
    }

    protected Rectangle GetDestRect(Rectangle? srcRect)
    {
        // Takes the transform's scale and the frame's origin into account so
        // the final dest rectangle lines up with what Draw actually renders.
        if (srcRect == null) return new Rectangle();

        int width  = (int)(srcRect.Value.Width  * Transform.Scale.X);
        int height = (int)(srcRect.Value.Height * Transform.Scale.Y);

        int posX = (int)(Transform.Position.X - _origin.X * Transform.Scale.X);
        int posY = (int)(Transform.Position.Y - _origin.Y * Transform.Scale.Y);

        return new Rectangle(posX, posY, width, height);
    }

    public virtual void Draw(SpriteBatch spriteBatch)
    {
        DestRect = GetDestRect(SourceRect);

        spriteBatch.Draw(
            Texture,
            Transform.Position,
            SourceRect,
            Color,
            MathHelper.ToRadians(Transform.Rotation),
            _origin,
            Transform.Scale,
            Effects,
            SortingOrder
        );
    }
}
