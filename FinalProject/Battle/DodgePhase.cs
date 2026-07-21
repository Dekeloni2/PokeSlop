using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FinalProject.Core;
using FinalProject.Core.Input;
using FinalProject.Core.Graphics;
using FinalProject.Battle.Patterns;
using System;

namespace FinalProject.Battle
{
    // runs one enemy attack: updates the bullet pattern, moves the soul,
    // handles collisions and damage
    public class DodgePhase
    {
        private readonly IBulletPattern _pattern;
        private readonly Teacher        _teacher;
        private readonly PlayerData     _playerData;
        private readonly PlayerHitbox   _hitbox;
        private readonly List<Projectile> _projectiles = new();
        private readonly List<Beam>       _beams       = new();
        private readonly List<HexHazard>  _hexes       = new();

        private readonly Rectangle   _baseBox;
        private readonly TweeningBox _box;
        private readonly ParticleSystem _particles = new();

        private float _elapsed;

        // ── camera, DRAW ONLY ────────────────────────────────────────────────
        // patterns pan/shake through DodgeContext and BattleState reads
        // CameraOffset for its draw transform. it never touches the actual
        // positions, beams/hexes/soul all stay in normal coords, so the camera
        // can't make a hitbox end up somewhere different from what you see
        private Vector2 _cameraPan;
        private Vector2 _shakeOffset;
        private float   _shakeMagnitude;
        private float   _shakeSeconds;
        private float   _shakeLeft;

        public Vector2 CameraOffset => _cameraPan + _shakeOffset;

        // patterns can hide the HP bar for their attack
        public bool HudHidden { get; private set; }

        // and the box outline, for attacks that take over the whole screen
        public bool BoxBorderHidden { get; private set; }

        // wait for leftover hazards to clear the arena before ending the turn,
        // otherwise the box starts shrinking back while hazards are live
        public bool IsFinished => _elapsed >= _pattern.Duration
                                  && _projectiles.Count == 0 && _beams.Count == 0 && _hexes.Count == 0;
        public Rectangle CurrentBox => _box.Current;

        internal float     Elapsed        => _elapsed;
        internal Rectangle BaseBox        => _baseBox;
        internal Vector2   HitboxPosition => _hitbox.Position;

        public DodgePhase(IBulletPattern pattern, Teacher teacher, PlayerData playerData, Rectangle baseBox)
        {
            _pattern    = pattern;
            _teacher    = teacher;
            _playerData = playerData;
            _baseBox    = baseBox;
            _box        = new TweeningBox(baseBox);
            _hitbox     = new PlayerHitbox(new Vector2(baseBox.Center.X, baseBox.Center.Y));

            _pattern.Start(new DodgeContext(this));
        }

        public void Update(GameTime gameTime, InputManager input)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _elapsed += dt;

            _box.Update(dt);

            _pattern.Update(gameTime, new DodgeContext(this));

            _hitbox.Update(gameTime, input, _box.Current);

            foreach (Projectile p in _projectiles)
                p.Update(gameTime, _box.Current);

            CheckCollisions();
            CheckBeamDamage(dt);

            foreach (HexHazard hex in _hexes)
                hex.Update(dt, _box.Current);
            CheckHexDamage();

            _particles.Update(dt);
            UpdateShake(dt);

            _projectiles.RemoveAll(p => p.IsExpired);
            _hexes.RemoveAll(h => h.IsFinished);
        }

        
        public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
        {
            if (_pattern is BoatPattern boatPattern)
            {
                Spritesheet boat = SpriteManager.GetSprite("boat");
                if (boat != null)
                {
                    int baseW = boat.Texture.Width * 2;
                    int baseH = boat.Texture.Height;

                    // Squash vertically (anchored to the keel line so it squanches
                    // down, not up) with a touch of widen for squash-&-stretch,
                    // then offset by the wind-up shake. All from the pattern's state.
                    float squash = boatPattern.SquashY;
                    int h = (int)(baseH * squash);
                    int w = (int)(baseW * (1f + (1f - squash) * 0.4f));

                    int keelY = CurrentBox.Bottom + 5 + baseH; // fixed bottom edge
                    int x = CurrentBox.Center.X - w / 2 + (int)boatPattern.ShakeOffset.X;
                    int y = keelY - h + (int)boatPattern.ShakeOffset.Y;

                    Rectangle boatRect = new Rectangle(x, y, w, h);
                    spriteBatch.Draw(boat.Texture, boatRect, boat[0, 0], boatPattern.BoatTint);
                }
            }
            
            if (_pattern is GarlicGunPattern garlic && (garlic.IsCharging || garlic.IsFiring || garlic.IsVanishing))
            {
                Rectangle laneRect = garlic.BeamStrip(CurrentBox);

                // tile several "!" markers across the lane instead of stretching
                // one, so it reads as a clear "danger here" strip, not a smear
                if (garlic.IsCharging)
                {
                    Spritesheet warning = SpriteManager.GetSprite("warning");
                    if (warning != null)
                    {
                        Rectangle frame = warning[garlic.WarningFrame, 0];

                        // draw each marker at its native pixel size (1:1) so point
                        // sampling stays crisp — scaling to a non-matching size is
                        // what made them look warped. Space them evenly along the lane.
                        int markerW = frame.Width;
                        int markerH = frame.Height;
                        int stride  = markerW + markerW / 3; // native width + a gap
                        int count   = laneRect.Width / stride;
                        if (count < 1) count = 1;
                        float step = laneRect.Width / (float)count;
                        int y = laneRect.Center.Y - markerH / 2;

                        // reveal the markers one at a time across the charge, from
                        // Vegeta's side outward (the way the beam will travel)
                        int shown = (int)(count * garlic.ChargeProgress) + 1;
                        if (shown > count) shown = count;

                        for (int i = 0; i < count; i++)
                        {
                            bool revealed = garlic.FromLeft ? i < shown : i >= count - shown;
                            if (!revealed) continue;

                            int cx = laneRect.Left + (int)(i * step + (step - markerW) / 2f);
                            spriteBatch.Draw(warning.Texture,
                                new Rectangle(cx, y, markerW, markerH), frame, Color.White);
                        }
                    }
                }

                // Vegeta stands just off the firing side, facing inward
                Spritesheet vegeta = SpriteManager.GetSprite("vegeta");
                if (vegeta != null)
                {
                    Rectangle src = vegeta[garlic.VegetaFrame % vegeta.Columns, 0];
                    int w = src.Width * 2, h = src.Height * 2;
                    int vy = laneRect.Center.Y - h / 2;
                    Rectangle dst = garlic.FromLeft
                        ? new Rectangle(CurrentBox.Left - w, vy, w, h)
                        : new Rectangle(CurrentBox.Right, vy, w, h);
                    SpriteEffects fx = garlic.FromLeft ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
                    spriteBatch.Draw(vegeta.Texture, dst, src, Color.White, 0f, Vector2.Zero, fx, 0f);
                }
            }

            if (!BoxBorderHidden)
                DrawBoxBorder(spriteBatch, pixel);

            foreach (Beam beam in _beams)
                beam.Draw(spriteBatch, pixel);

            // drawn after the beams so smoke shows in front of napoleon, but
            // before the bullets and the soul so those stay readable
            _particles.Draw(spriteBatch, pixel);

            foreach (HexHazard hex in _hexes)
                hex.Draw(spriteBatch, pixel);

            foreach (Projectile p in _projectiles)
                p.Draw(spriteBatch, pixel);

            _hitbox.Draw(spriteBatch);
            
            
        }

        // ── DodgeContext surface ─────────────────────────────────────────────

        internal void SpawnProjectile(Vector2 position, Vector2 velocity, ProjectileType type = ProjectileType.Normal)
            => _projectiles.Add(new Projectile(position, velocity,  type));

        internal Beam AddBeam(Rectangle bounds, Texture2D texture = null)
        {
            var beam = new Beam(bounds, texture);
            _beams.Add(beam);
            return beam;
        }

        internal void RemoveBeam(Beam beam) => _beams.Remove(beam);

        internal int HexCount => _hexes.Count;

        internal void SpawnHex(Vector2 center, float startRadius, float maxRadius,
            float rotation, float growSeconds, float explodeSpeed)
            => _hexes.Add(new HexHazard(center, startRadius, maxRadius, rotation, growSeconds, explodeSpeed));

        internal void ResizeBoxTo(Rectangle target, float overSeconds)
            => _box.ResizeTo(target, overSeconds);

        internal void SpawnParticle(Vector2 position, Vector2 velocity, float lifeSeconds,
                                    int size, Color color, float fadeInSeconds,
                                    Texture2D texture)
            => _particles.Spawn(position, velocity, lifeSeconds, size, color, fadeInSeconds, texture);

        // absolute offset from the resting position in px, + X moves the view right
        internal void SetCameraPan(Vector2 pan) => _cameraPan = pan;

        internal void ShakeScreen(float magnitude, float seconds)
        {
            _shakeMagnitude = magnitude;
            _shakeSeconds   = seconds;
            _shakeLeft      = seconds;
        }

        internal void SetHudHidden(bool hidden) => HudHidden = hidden;

        internal void SetBoxBorderHidden(bool hidden) => BoxBorderHidden = hidden;

        // random jitter that dies down over the shake duration
        private void UpdateShake(float dt)
        {
            if (_shakeLeft <= 0f)
            {
                _shakeOffset = Vector2.Zero;
                return;
            }

            _shakeLeft -= dt;
            float falloff = _shakeSeconds > 0f ? MathHelper.Clamp(_shakeLeft / _shakeSeconds, 0f, 1f) : 0f;
            float mag     = _shakeMagnitude * falloff;

            _shakeOffset = new Vector2(
                ((float)Random.Shared.NextDouble() * 2f - 1f) * mag,
                ((float)Random.Shared.NextDouble() * 2f - 1f) * mag);
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        // px and seconds of screen shake when the player gets hit
        private const float HitShakeMagnitude = 7f;
        private const float HitShakeSeconds   = 0.25f;

        // every hazard goes through here so they all share the same i-frames,
        // shake and damage instead of each doing its own thing
        private void HitPlayer(int amount)
        {
            if (_hitbox.IsInvulnerable) return;

            _playerData.TakeDamage(amount);
            _hitbox.TakeHit(); // starts the flash + invulnerability
            ShakeScreen(HitShakeMagnitude, HitShakeSeconds);
        }

        private void CheckCollisions()
        {
            if (_hitbox.IsInvulnerable) return;

            foreach (Projectile p in _projectiles)
            {
                if (p.IsExpired) continue;

                if (p.Bounds.Intersects(_hitbox.Bounds))
                {
                    HitPlayer(_teacher.Attack);
                    p.Expire(); // so the same bullet can't hit twice
                }
            }
        }

        // beams don't expire on contact — they deal a flat 1 damage on a fixed
        // cadence for as long as the soul stays inside them
        private void CheckBeamDamage(float dt)
        {
            if (_hitbox.IsInvulnerable) return;

            foreach (Beam beam in _beams)
            {
                // 1. Quick check: Are they even touching the beam's overall box?
                if (beam.Bounds.Intersects(_hitbox.Bounds))
                {
                    // 2. ONLY run pixel-perfect math if the active attack is the NapoleonPattern
                    if (_pattern is NapoleonPattern)
                    {
                        // Precise check: Is the player touching a solid pixel?
                        if (IntersectsPixel(beam.Bounds, beam.ColorData, _hitbox.Bounds))                        {
                            if (beam.Tick(dt, _hitbox.Bounds))
                                HitPlayer(Beam.Damage);
                        }
                    }
                    else
                    {
                        // Standard fast behavior for any other normal straight beams (like Garlic Gun)
                        if (beam.Tick(dt, _hitbox.Bounds))
                            HitPlayer(Beam.Damage);
                    }
                }
            }
        }

        private bool IntersectsPixel(Rectangle rectA, Color[] dataA, Rectangle rectB) // collision to white color only
        {
            // find the overlapping area
            int left = Math.Max(rectA.Left, rectB.Left);
            int right = Math.Min(rectA.Right, rectB.Right);
            int top = Math.Max(rectA.Top, rectB.Top);
            int bottom = Math.Min(rectA.Bottom, rectB.Bottom);

            if (left >= right || top >= bottom)
                return false;

            // get texture size
            var sprite = SpriteManager.GetSprite("napoleon");
            if (sprite == null)
                return false;

            int texWidth = sprite.Texture.Width;
            int texHeight = sprite.Texture.Height;

            // check every overlapping pixel
            for (int y = top; y < bottom; y++)
            {
                for (int x = left; x < right; x++)
                {
                    // convert screen position to texture position
                    int texX = (x - rectA.Left) * texWidth / rectA.Width;
                    int texY = (y - rectA.Top) * texHeight / rectA.Height;

                    Color pixel = dataA[texX + texY * texWidth];

                    // check if pixel is white
                    bool isWhite =
                        pixel.A > 0 &&
                        pixel.R > 240 &&
                        pixel.G > 240 &&
                        pixel.B > 240;

                    if (isWhite)
                        return true;
                }
            }

            return false;
        }

        // hexagon edges hurt continuously too, on their own damage cadence
        private void CheckHexDamage()
        {
            if (_hitbox.IsInvulnerable) return;

            foreach (HexHazard hex in _hexes)
                if (hex.TickDamage(_hitbox.Bounds))
                    HitPlayer(HexHazard.Damage);
        }

        private void DrawBoxBorder(SpriteBatch spriteBatch, Texture2D pixel)
        {
            const int borderThickness = 2;
            Rectangle boxRect = _box.Current;

            spriteBatch.Draw(pixel, new Rectangle(boxRect.X, boxRect.Y, boxRect.Width, borderThickness), Color.White);
            spriteBatch.Draw(pixel, new Rectangle(boxRect.X, boxRect.Bottom - borderThickness, boxRect.Width, borderThickness), Color.White);
            spriteBatch.Draw(pixel, new Rectangle(boxRect.X, boxRect.Y, borderThickness, boxRect.Height), Color.White);
            spriteBatch.Draw(pixel, new Rectangle(boxRect.Right - borderThickness, boxRect.Y, borderThickness, boxRect.Height), Color.White);
        }
    }
}
