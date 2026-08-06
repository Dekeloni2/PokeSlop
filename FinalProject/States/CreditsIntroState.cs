using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FinalProject.Core;
using FinalProject.Core.Audio;
using FinalProject.Core.Graphics;
using FinalProject.Core.StateMachine;

namespace FinalProject.States
{
    // The Undertale-style intro sting, played once — the very first time the
    // player reaches tiltan_hall (see OverworldState's pending-load handling,
    // which pushes this instead of fading straight in the first time, and
    // Game1.HasFirstCreditPlayed, which keeps it from firing ever again this
    // launch). mus_intronoise plays once per card on plain black — the title
    // card spells "Tilt" + the clover soul (standing in for the "a") + "nTale",
    // the credit card is plain text, then a third play of the sting carries a
    // last silent beat before popping. Pops itself when it's done —
    // OverworldState.Resume() picks up the normal fade-in from there, so this
    // reads as one continuous transition rather than a separate screen
    // bolted on.
    public class CreditsIntroState : GameState
    {
        private enum Phase { Title, Credit, Outro }

        // mus_intronoise runs ~2.84s — held a touch longer so it always
        // finishes before the card changes
        private const float CardHoldSeconds = 2.9f;

        private const string CreditText = "Created by Dekel Riess & Yonatan Medina";

        // "Tilt" + clover + "nTale", the clover standing in for the "a"
        private const string TitleLeft  = "Tilt";
        private const string TitleRight = "nTale";
        private const float  TitleTextScale = 3f;
        private const float  CloverScale    = 3f;   // inline with the word, not a separate logo
        private const int    CloverGap      = 10;    // px either side of the clover
        private const int    TitleY         = 220;

        private const float CreditTextScale = 2f;
        private const int   CreditY         = 230;

        private Phase _phase;
        private float _timer;

        public CreditsIntroState(Game1 game, GameStateManager stateManager)
            : base(game, stateManager) { }

        public override void OnEnter()
        {
            // the hallway's ambience would otherwise start behind the cards
            // the instant tiltan_hall finished loading — quiet for this
            SoundManager.StopMusic();
            EnterPhase(Phase.Title);
        }

        private void EnterPhase(Phase phase)
        {
            _phase = phase;
            _timer = 0f;
            SoundManager.Play("mus_intronoise");
        }

        public override void Update(GameTime gameTime)
        {
            _timer += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_timer < CardHoldSeconds) return;

            switch (_phase)
            {
                case Phase.Title:  EnterPhase(Phase.Credit); break;
                case Phase.Credit: EnterPhase(Phase.Outro);  break;
                case Phase.Outro:  StateManager.Pop();       break;
            }
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            Game.GraphicsDevice.Clear(Color.Black);

            if (_phase == Phase.Outro) return; // silent black beat, nothing to draw

            spriteBatch.Begin(samplerState: SamplerState.PointClamp);

            if (_phase == Phase.Title) DrawTitle(spriteBatch);
            else                       DrawCredit(spriteBatch);

            spriteBatch.End();
        }

        // "Tilt" + clover + "nTale" as one centred line. The clover isn't a
        // font glyph, so this is laid out by hand: measure each half, then
        // place clover-left / clover / clover-right left to right around a
        // shared centre instead of centring each piece independently.
        private void DrawTitle(SpriteBatch spriteBatch)
        {
            SpriteFont font = Game.DialogueFont;
            Spritesheet soul = SpriteManager.GetSprite("soul");

            Vector2 leftSize  = font.MeasureString(TitleLeft)  * TitleTextScale;
            Vector2 rightSize = font.MeasureString(TitleRight) * TitleTextScale;

            Rectangle cloverSrc = soul != null ? soul[0, 0] : Rectangle.Empty;
            int cloverW = soul != null ? (int)(cloverSrc.Width  * CloverScale) : 0;
            int cloverH = soul != null ? (int)(cloverSrc.Height * CloverScale) : 0;

            float totalWidth = leftSize.X + CloverGap + cloverW + CloverGap + rightSize.X;
            float startX = (GameSettings.WindowWidth - totalWidth) / 2f;

            var leftPos = new Vector2(startX, TitleY);
            spriteBatch.DrawString(font, TitleLeft, leftPos, Color.White,
                0f, Vector2.Zero, TitleTextScale, SpriteEffects.None, 0f);

            float cloverX = leftPos.X + leftSize.X + CloverGap;
            if (soul?.Texture != null)
            {
                // vertically centred against the text's own line height
                var dst = new Rectangle((int)cloverX,
                    (int)(TitleY + (leftSize.Y - cloverH) / 2f), cloverW, cloverH);
                spriteBatch.Draw(soul.Texture, dst, cloverSrc, Color.White);
            }

            var rightPos = new Vector2(cloverX + cloverW + CloverGap, TitleY);
            spriteBatch.DrawString(font, TitleRight, rightPos, Color.White,
                0f, Vector2.Zero, TitleTextScale, SpriteEffects.None, 0f);
        }

        private void DrawCredit(SpriteBatch spriteBatch)
        {
            SpriteFont font = Game.DialogueFont;
            Vector2 size = font.MeasureString(CreditText) * CreditTextScale;
            var pos = new Vector2((GameSettings.WindowWidth - size.X) / 2f, CreditY);

            spriteBatch.DrawString(font, CreditText, pos, Color.White,
                0f, Vector2.Zero, CreditTextScale, SpriteEffects.None, 0f);
        }
    }
}
