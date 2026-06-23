namespace FinalProject.Core;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public class Sprite : IGameDrawable
{
    protected Texture2D Texture { get; set; }

    public Sprite(Texture2D texture)
    {
        Texture = texture;
    }

    public virtual void Draw(SpriteBatch spriteBatch)
    {
    }
}
