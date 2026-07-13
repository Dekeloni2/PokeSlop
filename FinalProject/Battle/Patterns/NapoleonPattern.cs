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
            context.BaseBox.X, context.BaseBox.Y,
            context.BaseBox.Width, context.BaseBox.Height);

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

                int height = context.CurrentBox.Height;
                int width = context.CurrentBox.Width + 400;
                
                int startingX = context.CurrentBox.Left;

                Rectangle startingBounds = new Rectangle(
                    startingX,
                    context.CurrentBox.Top,
                    width + 100,
                    height + 100);
                
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