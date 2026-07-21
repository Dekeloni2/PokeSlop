using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FinalProject.Core.Graphics;

namespace FinalProject.Battle
{
    // the feedback right after you hit the teacher: the slash animation over
    // them, the damage number, and their HP bar. undertale only shows the
    // enemy's health in this moment instead of the whole fight.
    // BattleState holds it in the TurnFeedback phase so the turn waits for it
    public class DamageDisplay
    {
        private const float Duration      = 2.6f;  // how long the whole thing lasts
        private const float SlashSeconds  = 0.55f; // the attack animation
        private const float SlashScale    = 1.25f; // on top of fitting his height
        private const float RiseDistance  = 22f;   // px the number floats up
        private const float FadeStart     = 0.85f; // number starts fading at 85%
        private const float FlashInterval = 0.08f; // how fast the digits swap
        private const int   DigitSpacing  = 2;

        private const int BarWidth  = 180;
        private const int BarHeight = 18;

        // the two sets of digits on damage.png. they flash between each other,
        // set B is a couple px bigger because of the heavier outline.
        // note "1" is narrower than the rest so widths are per digit
        private static readonly Rectangle[] DigitsA =
        {
            new Rectangle(  4, 5, 28, 28), new Rectangle( 51, 5, 16, 28),
            new Rectangle( 86, 5, 28, 28), new Rectangle(127, 5, 28, 28),
            new Rectangle(168, 5, 28, 28), new Rectangle(209, 5, 28, 28),
            new Rectangle(250, 5, 28, 28), new Rectangle(291, 5, 28, 28),
            new Rectangle(332, 5, 28, 28), new Rectangle(373, 5, 28, 28),
        };

        private static readonly Rectangle[] DigitsB =
        {
            new Rectangle(413, 4, 30, 30), new Rectangle(460, 4, 18, 30),
            new Rectangle(495, 4, 30, 30), new Rectangle(536, 4, 30, 30),
            new Rectangle(577, 4, 30, 30), new Rectangle(618, 4, 30, 30),
            new Rectangle(659, 4, 30, 30), new Rectangle(700, 4, 30, 30),
            new Rectangle(741, 4, 30, 30), new Rectangle(782, 4, 30, 30),
        };

        // for a teacher that dodges everything and can only be ACTed past
        private static readonly Rectangle Miss = new Rectangle(823, 4, 118, 30);

        private bool  _active;
        private float _timer;

        private int _damage;
        private int _hp, _maxHp;
        private Rectangle _target; // where the teacher is on screen
        private Rectangle _box;    // the battle box, the HP bar sits on its top edge

        public bool IsFinished => !_active || _timer >= Duration;

        // true once the slash has played out. BattleState waits for this before
        // making the teacher react, so he recoils after the hit and not during it
        public bool SlashDone => _active && _timer >= SlashSeconds;

        public void Show(int damage, int hp, int maxHp, Rectangle target, Rectangle box)
        {
            _damage = damage;
            _hp     = hp;
            _maxHp  = maxHp;
            _target = target;
            _box    = box;
            _timer  = 0f;
            _active = true;
        }

        public void Hide() => _active = false;

        public void Update(float dt)
        {
            if (_active) _timer += dt;
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
        {
            if (!_active) return;

            DrawSlash(spriteBatch, SpriteManager.GetSprite("attackSlash"));

            // health and the number only come up once the slash has played out
            if (_timer < SlashSeconds) return;

            DrawHpBar(spriteBatch, pixel);
            DrawNumber(spriteBatch, SpriteManager.GetSprite("damageNumbers"));
        }

        // plays attack.png once over him, scaled to about his height
        private void DrawSlash(SpriteBatch spriteBatch, Spritesheet sheet)
        {
            if (sheet == null || _timer > SlashSeconds) return;

            int frame = (int)(_timer / SlashSeconds * sheet.Columns);
            if (frame >= sheet.Columns) frame = sheet.Columns - 1;

            Rectangle src = sheet[frame, 0];
            if (src.Height <= 0) return;

            float s = _target.Height / (float)src.Height * SlashScale;
            int   w = (int)(src.Width  * s);
            int   h = (int)(src.Height * s);

            var dst = new Rectangle(
                _target.Center.X - w / 2,
                _target.Center.Y - h / 2,
                w, h);

            spriteBatch.Draw(sheet.Texture, dst, src, Color.White);
        }

        private void DrawHpBar(SpriteBatch spriteBatch, Texture2D pixel)
        {
            // centred on the teacher and sitting just above his head
            int x = _target.Center.X - BarWidth / 2;
            int y = _target.Top - BarHeight - 4;
            if (y < 2) y = 2;

            // dark outline so it reads against whatever is behind it
            spriteBatch.Draw(pixel, new Rectangle(x - 2, y - 2, BarWidth + 4, BarHeight + 4), Color.Black);
            spriteBatch.Draw(pixel, new Rectangle(x, y, BarWidth, BarHeight), new Color(80, 20, 20));

            float pct = _maxHp > 0 ? MathHelper.Clamp((float)_hp / _maxHp, 0f, 1f) : 0f;
            spriteBatch.Draw(pixel,
                new Rectangle(x, y, (int)(BarWidth * pct), BarHeight),
                new Color(90, 220, 90));
        }

        // floats up from the middle of him, flashing between the two digit sets
        private void DrawNumber(SpriteBatch spriteBatch, Spritesheet sheet)
        {
            if (sheet == null) return;

            // timed from when the slash ends, not from the start, so the number
            // lands after the animation instead of over it
            float span = Duration - SlashSeconds;
            float t    = span > 0f ? MathHelper.Clamp((_timer - SlashSeconds) / span, 0f, 1f) : 1f;

            float alpha = t > FadeStart ? 1f - (t - FadeStart) / (1f - FadeStart) : 1f;
            Color tint  = Color.White * alpha;
            float rise  = -t * RiseDistance;

            // MISS only has the one version so it doesn't flash
            if (_damage <= 0)
            {
                var pos = new Vector2(
                    _target.Center.X - Miss.Width  / 2f,
                    _target.Center.Y - Miss.Height / 2f + rise);

                spriteBatch.Draw(sheet.Texture,
                    new Rectangle((int)pos.X, (int)pos.Y, Miss.Width, Miss.Height), Miss, tint);
                return;
            }

            bool flip = (int)(_timer / FlashInterval) % 2 == 1;
            Rectangle[] set = flip ? DigitsB : DigitsA;

            string digits = _damage.ToString();

            // measured per set so each one stays centred, the size difference
            // between them reads as a pulse
            int total = -DigitSpacing;
            foreach (char c in digits) total += set[c - '0'].Width + DigitSpacing;

            float x = _target.Center.X - total / 2f;
            float y = _target.Center.Y - set[0].Height / 2f + rise;

            foreach (char c in digits)
            {
                Rectangle src = set[c - '0'];
                spriteBatch.Draw(sheet.Texture,
                    new Rectangle((int)x, (int)y, src.Width, src.Height), src, tint);
                x += src.Width + DigitSpacing;
            }
        }
    }
}
