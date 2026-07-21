using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FinalProject.Core;
using FinalProject.Events;

namespace FinalProject.UI
{
    // the battle HP bar. keeps itself updated by subscribing to
    // PlayerHpChangedEvent instead of BattleState reading PlayerData every
    // frame. call Unsubscribe() when the fight ends or the handler sticks around
    public class BattleHud
    {
        private const int   BarX      = 40;
        private const int   BarY      = 390;
        private const float TextScale = 2f;

        private int _currentHp;
        private int _maxHp;

        // set from the current stats first, the event only fires when HP changes
        // so without this the bar would show 0 until you get hit
        public BattleHud(int currentHp, int maxHp)
        {
            _currentHp = currentHp;
            _maxHp     = maxHp;
            EventBus.Instance.Subscribe<PlayerHpChangedEvent>(OnHpChanged);
        }

        // unhook from the bus so the handler doesn't outlive the fight
        public void Unsubscribe()
            => EventBus.Instance.Unsubscribe<PlayerHpChangedEvent>(OnHpChanged);

        private void OnHpChanged(PlayerHpChangedEvent e)
        {
            _currentHp = e.CurrentHp;
            _maxHp     = e.MaxHp;
        }

        // drawn in every phase so you always see HP loss
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
