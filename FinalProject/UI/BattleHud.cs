using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FinalProject.Core;
using FinalProject.Events;

namespace FinalProject.UI
{
    // The battle HP readout. A UI element that keeps itself in sync by
    // subscribing to PlayerHpChangedEvent on the EventBus, instead of the
    // battle screen reading PlayerData every frame. Call Unsubscribe() when the
    // battle ends so the handler doesn't outlive the HUD.
    public class BattleHud
    {
        private const int   BarX      = 40;
        private const int   BarY      = 390;
        private const float TextScale = 2f;

        private int _currentHp;
        private int _maxHp;

        // Seeded from the current stats because the event only fires on a
        // *change* — without this the bar would read 0 until the first hit.
        public BattleHud(int currentHp, int maxHp)
        {
            _currentHp = currentHp;
            _maxHp     = maxHp;
            EventBus.Instance.Subscribe<PlayerHpChangedEvent>(OnHpChanged);
        }

        // Detach from the bus so this HUD (and its handler) can be collected
        // once the battle it belongs to is gone.
        public void Unsubscribe()
            => EventBus.Instance.Unsubscribe<PlayerHpChangedEvent>(OnHpChanged);

        private void OnHpChanged(PlayerHpChangedEvent e)
        {
            _currentHp = e.CurrentHp;
            _maxHp     = e.MaxHp;
        }

        // Drawn in every battle phase so HP loss is always visible.
        public void Draw(SpriteBatch spriteBatch, SpriteFont font, Texture2D pixel)
        {
            spriteBatch.DrawString(font, "HP", new Vector2(BarX, BarY), Color.White,
                0f, Vector2.Zero, TextScale, SpriteEffects.None, 0f);

            var barBg = new Rectangle(BarX + 60, BarY + 4, 100, 16);
            spriteBatch.Draw(pixel, barBg, new Color(60, 20, 20));

            float hpPercent = _maxHp > 0
                ? MathHelper.Clamp((float)_currentHp / _maxHp, 0f, 1f)
                : 0f;
            var fill = new Rectangle(barBg.X, barBg.Y, (int)(barBg.Width * hpPercent), barBg.Height);
            spriteBatch.Draw(pixel, fill, Color.Yellow);

            string hpText = $"{_currentHp} / {_maxHp}";
            spriteBatch.DrawString(font, hpText, new Vector2(barBg.Right + 16, BarY), Color.White,
                0f, Vector2.Zero, TextScale, SpriteEffects.None, 0f);
        }
    }
}
