using Microsoft.Xna.Framework;

namespace FinalProject.Battle.Patterns;

public class NapoleonPattern : IBulletPattern
{
    public float Duration => 10;

    private float _patternMoveSpeed = 20f;
    
    public void Start(DodgeContext context)
    {
        Rectangle box = context.CurrentBox;
        Rectangle hugeBox = new Rectangle(
            context.BaseBox.X - 50, context.BaseBox.Y - 50,
            context.BaseBox.Width + 100, context.BaseBox.Height + 100);
        context.ResizeBoxTo(hugeBox, 0.8f);
    }

    public void Update(GameTime gameTime, DodgeContext context)
    {
        
    }
}