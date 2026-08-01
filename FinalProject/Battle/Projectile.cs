using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FinalProject.Core;
using FinalProject.Core.Graphics;

namespace FinalProject.Battle
{

    public enum ProjectileType
    {
        Normal,
        Smoke, // ship attack
        Laser, // garlic gun
        Pixel, // yakir's single pixel
        Code,   // yakir's lessons, letters and numbers instead of bullets
        Word 
    }
    
    public class Projectile
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public bool IsExpired { get; private set; }

        public ProjectileType Type {get; private set;}
        
        public string Text { get; private set; }

        // the battle draws at 1:1, so a literal 2px pixel is invisible in
        // practice. 4px still reads as "one pixel" next to the 6px bullets,
        // and the halo below is what actually lets you find it
        private const int   PixelSize      = 4;
        private const int   PixelHaloSize  = 14;
        private const float PixelHaloAlpha = 0.28f;
        private const float PixelPulseHz   = 2.2f;

        private int _wordWidth;
        private int _wordHeight;
        
        public int ProjectileWidth => Type switch
        {
            ProjectileType.Smoke => 25,
            ProjectileType.Laser => 40,
            ProjectileType.Pixel => PixelSize,
            ProjectileType.Word  => _wordWidth,
            _ => GameSettings.DodgeProjectileSize // default size
        };
        
        public int ProjectileHeight => Type switch {
            ProjectileType.Smoke => 25,
            ProjectileType.Laser => 50,
            ProjectileType.Pixel => PixelSize,
            ProjectileType.Word  => _wordHeight,
            _ => GameSettings.DodgeProjectileSize // default size
        };
        
        public Rectangle Bounds => new Rectangle(
            (int)Position.X - ProjectileWidth / 2,
            (int)Position.Y - ProjectileHeight / 2,
            ProjectileWidth,
            ProjectileHeight);

        // mostly ones and zeros with a few identifiers mixed in, so the rain
        // reads as code at a glance instead of turning into alphabet soup
        private const string CodeGlyphs = "0101010101123456789ifxnbFIXNB";

        // drawn a little larger than the hitbox so a near miss looks like a
        // near miss, and glyphs stay legible at this resolution
        private const float CodeGlyphHeight = 11f;

        private readonly char _glyph;

        public Projectile(Vector2 position, Vector2 velocity, ProjectileType type = ProjectileType.Normal) // added defaults
        {
            Position = position;
            Velocity = velocity;
            Type = type;

            if (type == ProjectileType.Code)
                _glyph = CodeGlyphs[Random.Shared.Next(CodeGlyphs.Length)];
        }
        
        public float Scale { get; private set; } = 1.0f; // added default scale 
        
        public Projectile(Vector2 position, Vector2 velocity, string text, SpriteFont font, float scale = 0.85f)
            : this(position, velocity, ProjectileType.Word)
        {
            Text = text ?? string.Empty;
            Scale = scale;

            if (font != null && !string.IsNullOrEmpty(Text))
            {
                Vector2 measured = font.MeasureString(Text) * scale;
                _wordWidth = (int)Math.Ceiling(measured.X);
                _wordHeight = (int)Math.Ceiling(measured.Y);
            }
            else
            {
                _wordWidth = (int)(Text.Length * 8 * Scale);
                _wordHeight = (int)(14 * Scale);
            }
        }
        
        private float _lifeTime;

        // expires once it's fully outside the current box (plus a margin)
        public void Update(GameTime gameTime, Rectangle box)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            Position  += Velocity * dt;
            _lifeTime += dt;
            
            Rectangle expireBounds = new Rectangle(
                box.X - GameSettings.DodgeBoundsMargin,
                box.Y - GameSettings.DodgeBoundsMargin,
                box.Width  + GameSettings.DodgeBoundsMargin * 2,
                box.Height + GameSettings.DodgeBoundsMargin * 2);
            
            if (!expireBounds.Contains(Position.ToPoint()))
                IsExpired = true;
        }

        // called on hit so the same bullet can't hit twice
        public void Expire() => IsExpired = true;

        public void Draw(SpriteBatch spriteBatch, Texture2D pixel, SpriteFont font = null)
        {
            // yakir's bullets are the code he's teaching. falls back to a plain
            // square if no font came through, so it can never draw nothing
            if (Type == ProjectileType.Code && font != null)
            {
                string text  = _glyph.ToString();
                Vector2 size = font.MeasureString(text);
                float scale  = size.Y > 0f ? CodeGlyphHeight / size.Y : 1f;

                spriteBatch.DrawString(font, text, Position, Color.OrangeRed,
                    0f, size / 2f, scale, SpriteEffects.None, 0f);
                return;
            }

            if (Type == ProjectileType.Laser)
            {
                var sheet = SpriteManager.GetSprite("garlicGun");
                if (sheet != null)
                {
                    spriteBatch.Draw(sheet.Texture, Bounds, Color.White);
                }
                return;
            }
            
            // the one pixel gets a soft pulsing halo behind it. the pixel itself
            // is still tiny, the glow is just so the player can track it
            if (Type == ProjectileType.Pixel)
            {
                float pulse = 0.6f + 0.4f * (float)System.Math.Sin(_lifeTime * MathHelper.TwoPi * PixelPulseHz);

                spriteBatch.Draw(pixel, new Rectangle(
                    (int)Position.X - PixelHaloSize / 2,
                    (int)Position.Y - PixelHaloSize / 2,
                    PixelHaloSize, PixelHaloSize), Color.White * (PixelHaloAlpha * pulse));
            }
            
            if (Type == ProjectileType.Word)
            {
                if (font != null && !string.IsNullOrEmpty(Text))
                {
                    Vector2 textSize = font.MeasureString(Text);
                    Vector2 origin = textSize / 2f;

                    spriteBatch.DrawString(
                        font,
                        Text,
                        Position,
                        new Color(176, 191, 26), // shrek green
                        0f,
                        origin,
                        Scale,
                        SpriteEffects.None,
                        0f
                    );
                    return;
                }
                else
                {
                    spriteBatch.Draw(pixel, Bounds, Color.Yellow);
                }
                return;
            }

            Color renderColor = Type switch
            {
                ProjectileType.Smoke => Color.Gray * 0.6f,
                ProjectileType.Pixel => Color.White, // just the one pixel
                _ => Color.OrangeRed
            };

            spriteBatch.Draw(pixel, Bounds, renderColor);
        }
        
    }
}
