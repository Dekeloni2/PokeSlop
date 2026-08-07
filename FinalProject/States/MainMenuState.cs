using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using FinalProject.Core;
using FinalProject.Core.Audio;
using FinalProject.Core.StateMachine;
using FinalProject.UI;

namespace FinalProject.States
{
    // First screen the player sees.
    public class MainMenuState : GameState
    {
        private static readonly Color BackgroundColor = Color.Black;
        private static readonly Color HeaderColor     = Color.Gray;
        private static readonly Color TextColor       = Color.White;
        private static readonly Color SelectedColor   = Color.Yellow;

        private const float BodyScale   = 2.4f;
        private const float OptionScale = 2.6f;
        private const float HintScale   = 1.8f;

        private const int LeftMargin = 140;
        private const int TopMargin  = 40;

        private enum ScreenState { Main, Settings }
        private ScreenState _currentScreen = ScreenState.Main;

        private readonly string[] _menuOptions = { "Begin Game", "Settings" };
        private int _selectedIndex = 0;

        // "Begin Game" doesn't cut to the overworld directly — the screen
        // fades to white over mus_cymbal (~5.19s), then swaps the instant
        // it's fully white, so the actual state change lands on a blank
        // frame rather than a visible pop
        private const float StartGameFadeSeconds = 5.3f;
        private bool  _startingGame;
        private float _startGameFadeT;

        // shared with OverworldMenu's in-game C-menu settings, so the same
        // knobs behave identically whether opened before or during a run
        private readonly SettingsMenu _settings;

        public MainMenuState(Game1 game, GameStateManager stateManager)
            : base(game, stateManager)
        {
            _settings = new SettingsMenu(game);
        }

        public override void OnEnter()
        {
            _selectedIndex  = 0;
            _currentScreen  = ScreenState.Main;
            _startingGame   = false;
            _startGameFadeT = 0f;

            //StateManager.Replace(new OverworldState(Game, StateManager)); //---------------------------------remove comment to skip the menu--------------------------
        }

        public override void Update(GameTime gameTime)
        {
            // owns input outright while it plays — no backing out of a
            // fade that's already committed to starting the run
            if (_startingGame)
            {
                _startGameFadeT += (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (_startGameFadeT >= StartGameFadeSeconds)
                    StateManager.Replace(new OverworldState(Game, StateManager));
                return;
            }

            if (Game.Input.IsKeyPressed(Keys.X) || Game.Input.IsKeyPressed(Keys.RightShift) || Game.Input.IsKeyPressed(Keys.Escape))
            {
                if (_currentScreen == ScreenState.Settings)
                {
                    SoundManager.Play(SoundManager.MenuSelect);
                    _currentScreen = ScreenState.Main;
                    _selectedIndex = 1; // Highlight "Settings" on main menu
                    return;
                }
            }

            if (_currentScreen == ScreenState.Settings)
            {
                UpdateSettings();
                return;
            }

            UpdateMain();
        }

        private void UpdateMain()
        {
            if (Game.Input.IsKeyPressed(Keys.Up))
            {
                _selectedIndex--;
                if (_selectedIndex < 0)
                    _selectedIndex = _menuOptions.Length - 1;

                SoundManager.Play(SoundManager.MenuMove);
            }

            if (Game.Input.IsKeyPressed(Keys.Down))
            {
                _selectedIndex++;
                if (_selectedIndex >= _menuOptions.Length)
                    _selectedIndex = 0;

                SoundManager.Play(SoundManager.MenuMove);
            }

            if (Game.Input.IsKeyPressed(Keys.Z) || Game.Input.IsKeyPressed(Keys.Enter))
            {
                SoundManager.Play(SoundManager.MenuSelect);

                if (_selectedIndex == 0)
                {
                    _startingGame   = true;
                    _startGameFadeT = 0f;
                    SoundManager.Play("mus_cymbal");
                }
                else if (_selectedIndex == 1)
                {
                    _currentScreen = ScreenState.Settings;
                    _settings.ResetSelection();
                }
            }
        }

        private void UpdateSettings()
        {
            if (Game.Input.IsKeyPressed(Keys.Up))    _settings.MoveUp();
            if (Game.Input.IsKeyPressed(Keys.Down))  _settings.MoveDown();
            if (Game.Input.IsKeyPressed(Keys.Left))  _settings.Adjust(-1);
            if (Game.Input.IsKeyPressed(Keys.Right)) _settings.Adjust(1);

            if (Game.Input.IsKeyPressed(Keys.Z) || Game.Input.IsKeyPressed(Keys.Enter))
            {
                SoundManager.Play(SoundManager.MenuSelect);

                if (_settings.SelectedIndex == SettingsMenu.BackIndex)
                {
                    _currentScreen = ScreenState.Main;
                    _selectedIndex = 1;
                }
                else
                {
                    _settings.Adjust(1);
                }
            }
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            Game.GraphicsDevice.Clear(BackgroundColor);

            spriteBatch.Begin(samplerState: SamplerState.PointClamp);

            string header = (_currentScreen == ScreenState.Main) ? "--- Instruction ---" : "--- Settings ---";
            DrawCentered(spriteBatch, header, TopMargin, HeaderColor, BodyScale);

            int lineY = TopMargin + 50;

            if (_currentScreen == ScreenState.Main)
            {
                int lineHeight = 30;

                string[] instructions = new string[]
                {
                    "[Z or ENTER] - Confirm",
                    "[X] - Cancel",
                    "[C] - Menu (In-game)",
                    "[F11] - Toggle Fullscreen",
                    "[Hold ESC] - Quit",
                    "When HP is 0, you lose."
                };

                foreach (string line in instructions)
                {
                    DrawText(spriteBatch, line, new Vector2(LeftMargin, lineY), TextColor, BodyScale);
                    lineY += lineHeight;
                }

                lineY += 25;
                for (int i = 0; i < _menuOptions.Length; i++)
                {
                    bool isSelected = (i == _selectedIndex);
                    Color color = isSelected ? SelectedColor : TextColor;

                    DrawText(spriteBatch, _menuOptions[i], new Vector2(LeftMargin, lineY), color, OptionScale);
                    lineY += 36;
                }
            }

            else if (_currentScreen == ScreenState.Settings)
            {
                int lineHeight = 40;

                for (int i = 0; i < SettingsMenu.Labels.Length; i++)
                {
                    bool isSelected = (i == _settings.SelectedIndex);
                    Color color = isSelected ? SelectedColor : TextColor;

                    DrawText(spriteBatch, _settings.DisplayFor(i), new Vector2(LeftMargin, lineY), color, OptionScale);
                    lineY += lineHeight;
                }

                // Draw helper key tip at bottom
                DrawText(spriteBatch, "[LEFT/RIGHT] Adjust    [X] Back",
                    new Vector2(LeftMargin, lineY + 30), Color.DarkGray, HintScale);
            }

            // over everything above, growing to solid white as the run starts
            if (_startingGame)
            {
                float t = MathHelper.Clamp(_startGameFadeT / StartGameFadeSeconds, 0f, 1f);
                spriteBatch.Draw(Game.PixelTexture,
                    new Rectangle(0, 0, GameSettings.WindowWidth, GameSettings.WindowHeight),
                    Color.White * t);
            }

            spriteBatch.End();
        }

        private void DrawText(SpriteBatch spriteBatch, string text, Vector2 position, Color color, float scale)
        {
            spriteBatch.DrawString(
                Game.DialogueFont,
                text,
                position,
                color,
                0f,
                Vector2.Zero,
                scale,
                SpriteEffects.None,
                0f
            );
        }

        private void DrawCentered(SpriteBatch spriteBatch, string text, float y, Color color, float scale)
        {
            float width = Game.DialogueFont.MeasureString(text).X * scale;
            DrawText(spriteBatch, text, new Vector2((GameSettings.WindowWidth - width) / 2f, y), color, scale);
        }
    }
}
