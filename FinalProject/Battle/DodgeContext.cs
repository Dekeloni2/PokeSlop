using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using FinalProject.Data;

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

        // the wide menu box, for attacks that want the arena to take that shape
        // rather than hardcoding its geometry a second time
        public Rectangle MenuBox        => _phase.MenuBox;
        public Vector2   HitboxPosition => _phase.HitboxPosition;

        // for undertale style blue/orange attacks: blue only hurts you while
        // you're moving, orange only while you're standing still
        public bool      IsPlayerMoving => _phase.HitboxIsMoving;

        // ── grid movement ────────────────────────────────────────────────────
        // takes free movement away and steps the soul tile by tile instead, for
        // the chess board. the soul is placed by tile, so it ignores the usual
        // box clamp while this is on
        public Point SoulTile => _phase.HitboxTile;

        public void EnterGrid(Point originPx, int tileSize, int cols, int rows, Point startTile)
            => _phase.EnterGrid(originPx, tileSize, cols, rows, startTile);
        public void ExitGrid() => _phase.ExitGrid();

        // shove the soul onto a given tile, for knockback. a recoil wants to be
        // faster than a normal step, hence the slide override
        public void MoveSoulToTile(Point tile, float slideSeconds = 0.09f)
            => _phase.MoveSoulToTile(tile, slideSeconds);

        // ignore movement input for a moment, without moving the soul
        public void HoldSoul(float seconds) => _phase.HoldSoul(seconds);

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

        // for attacks that hurt the player through something other than a
        // hazard, like getting a quiz answer wrong. goes through the same
        // i-frames and shake as everything else
        public void DamagePlayer(int amount) => _phase.HitPlayer(amount);

        // the other direction, as a fraction of max HP (0.2f == a fifth of the bar)
        public void HealPlayer(float fractionOfMax) => _phase.HealPlayer(fractionOfMax);

        // drops the soul back in the middle of the arena
        public void CenterHitbox() => _phase.CenterHitbox();

        // wipes every live bullet at once, for the loop lesson's ctrl+c gag
        public void ClearProjectiles() => _phase.ClearProjectiles();

        // running total of hits taken this turn, for attacks that end early
        // once they've actually landed
        public int PlayerHitCount => _phase.PlayerHitCount;

        // resize the arena, DodgePhase animates the transition
        public void ResizeBoxTo(Rectangle target, float overSeconds)
            => _phase.ResizeBoxTo(target, overSeconds);

        // visual only, these never damage the player. pass a texture for a
        // sprite (like "smoke"), null draws a plain square
        public void SpawnParticle(Vector2 position, Vector2 velocity, float lifeSeconds,
                                  int size, Color color, float fadeInSeconds = 0.3f,
                                  Texture2D texture = null)
            => _phase.SpawnParticle(position, velocity, lifeSeconds, size, color, fadeInSeconds, texture);

        // ── camera, visual only, doesn't affect collision ────────────────────

        // absolute offset in px, + X moves the view right
        public void SetCameraPan(Vector2 pan) => _phase.SetCameraPan(pan);

        // one shot screen shake that dies down over its duration
        public void ShakeScreen(float magnitude, float seconds)
            => _phase.ShakeScreen(magnitude, seconds);

        // hide the HP bar for the rest of the attack
        public void SetHudHidden(bool hidden) => _phase.SetHudHidden(hidden);

        // hide the box outline, for attacks that cover the whole screen
        public void SetBoxBorderHidden(bool hidden) => _phase.SetBoxBorderHidden(hidden);

        // put the teacher on screen during the attack and give him a line
        public void SetTeacherVisible(bool visible) => _phase.SetTeacherVisible(visible);
        public void SetTeacherSpeech(string text)   => _phase.SetTeacherSpeech(text);

        // the teacher's own multi part rig, for attacks that draw and animate
        // his actual body (see PunchPattern) instead of a separate hazard or
        // the passive floating idle. null if his JSON has no "sprite" block
        public TeacherSpriteData TeacherSprite => _phase.TeacherSpriteData;

        // a line authored in the teacher's JSON for a named beat of this attack,
        // falling back to what the pattern passes when the file doesn't cover it.
        // lets characterisation be rewritten without a rebuild, while text the
        // mechanic depends on (quiz prompts) stays in the pattern where it belongs.
        // variant picks between lines for the same beat — see MoveData.Line
        public string Line(string beat, int variant, string fallback)
            => _phase.Line(beat, variant, fallback);

        public bool IsKeyDown(Keys key) => _phase.IsKeyDown(key);
    }
}
