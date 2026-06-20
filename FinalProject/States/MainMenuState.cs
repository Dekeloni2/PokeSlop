// States/MainMenuState.cs
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using FinalProject.Core;

namespace FinalProject.States
{
    // First screen the player sees.
    // Press Enter to move into the overworld.
    // Placeholder visuals until we add sprites and fonts.
    public class MainMenuState : GameState
    {
        private static readonly Color BackgroundColor = new Color(15, 15, 25);
        private static readonly Color TitleBarColor   = new Color(40, 40, 60);

        private Rectangle _titleBarRect;

        public MainMenuState(Game1 game, GameStateManager stateManager)
            : base(game, stateManager) { }

        public override void OnEnter()
        {
            _titleBarRect = new Rectangle(
                0,
                GameSettings.WindowHeight / 4,
                GameSettings.WindowWidth,
                GameSettings.WindowHeight / 3
            );

            // For quick debugging, immediately enter the overworld so we can
            // verify the map loads and the player is visible without user input.
            StateManager.Replace(new OverworldState(Game, StateManager));
        }

        public override void Update(GameTime gameTime)
        {
            if (Game.Input.IsKeyPressed(Keys.Enter))
                StateManager.Replace(new OverworldState(Game, StateManager));
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            Game.GraphicsDevice.Clear(BackgroundColor);

            spriteBatch.Begin();

            // Placeholder title bar — replace with a real title screen sprite later
            spriteBatch.Draw(Game.PixelTexture, _titleBarRect, TitleBarColor);

            // TODO: spriteBatch.DrawString(font, "Press Enter", position, Color.White);

            spriteBatch.End();
        }
    }
}