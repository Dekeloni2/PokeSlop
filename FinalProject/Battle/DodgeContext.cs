using Microsoft.Xna.Framework;

namespace FinalProject.Battle
{
    // what a pattern is allowed to touch. Patterns can't reach PlayerData or
    // the projectile list directly, damage stays inside DodgePhase.
    public class DodgeContext
    {
        private readonly DodgePhase _phase;

        internal DodgeContext(DodgePhase phase) => _phase = phase;

        public float     Elapsed        => _phase.Elapsed;
        public Rectangle BaseBox        => _phase.BaseBox;
        public Rectangle CurrentBox     => _phase.CurrentBox;
        public Vector2   HitboxPosition => _phase.HitboxPosition;

        public void SpawnProjectile(Vector2 position, Vector2 velocity, ProjectileType type = ProjectileType.Normal)
            => _phase.SpawnProjectile(position, velocity,  type);

        // resize the arena, DodgePhase animates the transition
        public void ResizeBoxTo(Rectangle target, float overSeconds)
            => _phase.ResizeBoxTo(target, overSeconds);
    }
}
