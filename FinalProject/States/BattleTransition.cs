using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FinalProject.Battle;
using FinalProject.Core;
using FinalProject.Core.Graphics;
using FinalProject.Core.StateMachine;
using FinalProject.Entities;

namespace FinalProject.States
{
    // The Undertale-style battle intro, on a pure black background:
    //   1. the soul (clover) appears ON the still-visible player and blinks;
    //   2. the player is removed;
    //   3. the soul lerps down onto the FIGHT button.
    // Pushed in place of BattleState and replaces itself with the battle once
    // the animation finishes, so the overworld stays paused underneath and
    // resumes when the battle ends.
    public class BattleTransition : GameState
    {
        private readonly Teacher _teacher;
        private readonly Player  _player;
        private readonly float   _playerScale;
        private readonly Vector2 _soulStart;   // player's screen position
        private readonly Vector2 _soulTarget;  // the FIGHT button

        private float _timer;

        private const float AppearSeconds = 0.5f;  // soul on the player, blinking
        private const float LerpSeconds   = 0.4f;  // soul flies to the button
        private const float BlinkHz       = 16f;   // soul blink rate while appearing

        public BattleTransition(Game1 game, GameStateManager sm, Teacher teacher,
            Player player, Vector2 soulStartScreenPos, float playerScale)
            : base(game, sm)
        {
            _teacher     = teacher;
            _player      = player;
            _soulStart   = soulStartScreenPos;
            _playerScale = playerScale;
            _soulTarget  = BattleMenu.FightSoulPosition();
        }

        public override void Update(GameTime gameTime)
        {
            _timer += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_timer >= AppearSeconds + LerpSeconds)
                StateManager.Replace(new BattleState(Game, StateManager, _teacher));
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            Game.GraphicsDevice.Clear(Color.Black);
            spriteBatch.Begin(samplerState: SamplerState.PointClamp);

            Spritesheet soul = SpriteManager.GetSprite("soul");
            Rectangle soulSrc = soul != null ? soul[0, 0] : Rectangle.Empty;

            if (_timer < AppearSeconds)
            {
                // the frozen player is still on screen; the soul sits on top of
                // it and blinks
                _player.DrawFrozen(spriteBatch, _soulStart, _playerScale);

                bool blinkOn = (int)(_timer * BlinkHz) % 2 == 0;
                if (blinkOn && soul != null)
                    DrawSoul(spriteBatch, soul, soulSrc, _soulStart);
            }
            else
            {
                // player removed; the soul eases down to the FIGHT button
                float t = MathHelper.Clamp((_timer - AppearSeconds) / LerpSeconds, 0f, 1f);
                t = t * t * (3f - 2f * t); // smoothstep
                Vector2 pos = Vector2.Lerp(_soulStart, _soulTarget, t);
                if (soul != null)
                    DrawSoul(spriteBatch, soul, soulSrc, pos);
            }

            spriteBatch.End();
        }

        private static void DrawSoul(SpriteBatch sb, Spritesheet soul, Rectangle src, Vector2 center)
        {
            sb.Draw(soul.Texture,
                new Rectangle((int)center.X - src.Width / 2, (int)center.Y - src.Height / 2, src.Width, src.Height),
                src, Color.White);
        }
    }
}
