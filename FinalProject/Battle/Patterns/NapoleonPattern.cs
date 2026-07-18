using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FinalProject.Core.Graphics;

namespace FinalProject.Battle.Patterns;

public class NapoleonPattern : IBulletPattern
{
    public float Duration => 10;

    private float _patternMoveSpeed = 80f; 
    private bool _hasSetup = false;
    private Beam _activeBeam;

    public void Start(DodgeContext context)
    {
        _hasSetup = false;
        
        // box size
        Rectangle hugeBox = new Rectangle(
            context.BaseBox.X - 80, context.BaseBox.Y - 50,
            context.BaseBox.Width + 160, context.BaseBox.Height + 100);

        context.ResizeBoxTo(hugeBox, 0.8f);
    }

    public void Update(GameTime gameTime, DodgeContext context)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // wait time before begin( add warnings??)
        if (context.Elapsed < 1f)
            return;

        // spawn the sprite 
        if (!_hasSetup)
        {
            Spritesheet napoleonSprite = SpriteManager.GetSprite("napoleon");

            if (napoleonSprite != null)
            {
                Texture2D tex = napoleonSprite.Texture;
                
                int width = (int)(context.CurrentBox.Width * 1.6f);
                int height = (int)(context.CurrentBox.Height * 1.3f);
                
                int startingX = context.CurrentBox.Right;

                Rectangle startingBounds = new Rectangle(
                    startingX,
                    context.CurrentBox.Top,
                    width,
                    height);
                
                _activeBeam = context.AddBeam(startingBounds, tex);
            }

            _hasSetup = true;
        }

        // moving the sprite
        if (_activeBeam != null)
        {
            Rectangle bounds = _activeBeam.Bounds;
            int nextX = bounds.X - (int)(_patternMoveSpeed * dt);

            _activeBeam.Bounds = new Rectangle(nextX, bounds.Y, bounds.Width, bounds.Height);

            // if slides completly (might delete later)
            if (_activeBeam.Bounds.Right < context.CurrentBox.Left)
            {
                context.RemoveBeam(_activeBeam);
                _activeBeam = null;
            }
        }
    }
}