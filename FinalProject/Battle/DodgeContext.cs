using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

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

        // a persistent damaging beam. Returns a handle so the pattern can move
        // or remove it; DodgePhase draws it and applies its continuous damage.
        public Beam AddBeam(Rectangle bounds, Texture2D texture = null) => _phase.AddBeam(bounds, texture);
        public void RemoveBeam(Beam beam)     => _phase.RemoveBeam(beam);

        // hexagon hazards — DodgePhase owns them, grows/draws them and applies
        // their continuous edge damage; the pattern just spawns and counts them
        public int  HexCount => _phase.HexCount;
        public void SpawnHex(Vector2 center, float startRadius, float maxRadius,
            float rotation, float growSeconds, float explodeSpeed)
            => _phase.SpawnHex(center, startRadius, maxRadius, rotation, growSeconds, explodeSpeed);

        // resize the arena, DodgePhase animates the transition
        public void ResizeBoxTo(Rectangle target, float overSeconds)
            => _phase.ResizeBoxTo(target, overSeconds);

        // purely decorative specks — they never damage the player. Pass a
        // texture to use a sprite (e.g. "smoke"); null draws a plain square.
        public void SpawnParticle(Vector2 position, Vector2 velocity, float lifeSeconds,
                                  int size, Color color, float fadeInSeconds = 0.3f,
                                  Texture2D texture = null)
            => _phase.SpawnParticle(position, velocity, lifeSeconds, size, color, fadeInSeconds, texture);

        // ── Battle camera (visual only, never affects collision) ─────────────

        // Absolute camera offset in pixels. Positive X shifts the view right,
        // which slides the content left on screen.
        public void SetCameraPan(Vector2 pan) => _phase.SetCameraPan(pan);

        // One-shot screen shake that decays over its duration.
        public void ShakeScreen(float magnitude, float seconds)
            => _phase.ShakeScreen(magnitude, seconds);

        // Hide the HP readout for the rest of this attack.
        public void SetHudHidden(bool hidden) => _phase.SetHudHidden(hidden);

        // Hide the arena outline — for attacks that fill the whole screen.
        public void SetBoxBorderHidden(bool hidden) => _phase.SetBoxBorderHidden(hidden);
    }
}
