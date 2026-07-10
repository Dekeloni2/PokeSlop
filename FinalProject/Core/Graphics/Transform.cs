namespace FinalProject.Core.Graphics;

using Microsoft.Xna.Framework;

// Position/rotation/scale for a Sprite. Rotation is in degrees, converted to
// radians only at draw time (see Sprite.Draw).
public class Transform
{
    public Vector2 Position { get; set; } = Vector2.Zero;
    public float   Rotation { get; set; } = 0f;
    public Vector2 Scale    { get; set; } = Vector2.One;
}
