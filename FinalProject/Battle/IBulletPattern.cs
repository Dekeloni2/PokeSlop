using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FinalProject.Battle
{
    // the bullet choreography for one enemy move. A fresh instance is made
    // every turn since patterns keep their own timers.
    public interface IBulletPattern
    {
        float Duration { get; }

        // patterns that can finish on their own terms override this — the chess
        // board ends the moment the last piece is taken, rather than sitting
        // there for the rest of its Duration. everything else just runs out the
        // clock like before, which is why it has a default
        bool IsComplete => false;

        void Start(DodgeContext context);
        void Update(GameTime gameTime, DodgeContext context);
        
        void Draw(SpriteBatch spriteBatch, Texture2D pixel, DodgeContext context) { } // added for onion rings
    }
}
