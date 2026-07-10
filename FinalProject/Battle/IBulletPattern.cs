using Microsoft.Xna.Framework;

namespace FinalProject.Battle
{
    // the bullet choreography for one enemy move. A fresh instance is made
    // every turn since patterns keep their own timers.
    public interface IBulletPattern
    {
        float Duration { get; }

        void Start(DodgeContext context);
        void Update(GameTime gameTime, DodgeContext context);
    }
}
