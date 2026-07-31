using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using FinalProject.Core;
using FinalProject.Core.StateMachine;

namespace FinalProject.States
{
    // First screen the player sees.
    // Press Enter to move into the overworld.
    // Placeholder visuals until we add sprites and fonts.
    public class MainMenuState : GameState
    {
        private static readonly Color BackgroundColor = Color.Black;
        private static readonly Color HeaderColor     = Color.Gray;
        private static readonly Color TextColor       = Color.White;
        private static readonly Color SelectedColor   = Color.Yellow;
        
        private enum ScreenState { Main, Settings }
        private ScreenState _currentScreen = ScreenState.Main;
        
        private readonly string[] _menuOptions = { "Begin Game", "Settings" };
        private readonly string[] _settingsOptions = { "Sound Volume", "FPS Target", "Back" };
        private int _selectedIndex = 0;
        
        private int _volume = 80;     // 0% to 100%
        private int[] _fpsOptions = { 30, 60 };
        private int _fpsIndex = 1;    // Defaults to 60 FPS

        public MainMenuState(Game1 game, GameStateManager stateManager)
            : base(game, stateManager) { }

        public override void OnEnter()
        {
            _selectedIndex = 0;
            _volume = GameSettings.MasterVolume;
            _fpsIndex = (GameSettings.TargetFps == 30) ? 0 : 1;
            
            //StateManager.Replace(new OverworldState(Game, StateManager)); //---------------------------------remove comment to skip the menu--------------------------
        }

        public override void Update(GameTime gameTime)
        {
            if (Game.Input.IsKeyPressed(Keys.X) || Game.Input.IsKeyPressed(Keys.Escape))
            {
                if (_currentScreen == ScreenState.Settings)
                {
                    _currentScreen = ScreenState.Main;
                    _selectedIndex = 1; // Highlight "Settings" on main menu
                    return;
                }
            }
            
            int optionCount = (_currentScreen == ScreenState.Main) ? _menuOptions.Length : _settingsOptions.Length;
            
            // Cycle Up
            if (Game.Input.IsKeyPressed(Keys.Up))
            {
                _selectedIndex--;
                if (_selectedIndex < 0)
                    _selectedIndex = optionCount - 1;
            }

            // Cycle Down
            if (Game.Input.IsKeyPressed(Keys.Down))
            {
                _selectedIndex++;
                if (_selectedIndex >= optionCount)
                    _selectedIndex = 0;
            }

            if (_currentScreen == ScreenState.Settings)
            {
                if (Game.Input.IsKeyPressed(Keys.Left))
                {
                    AdjustSetting(-1);
                }
                if (Game.Input.IsKeyPressed(Keys.Right))
                {
                    AdjustSetting(1);
                }
            }
            
            // Confirm Selection
            if (Game.Input.IsKeyPressed(Keys.Z) || Game.Input.IsKeyPressed(Keys.Enter))
            {
                if (_currentScreen == ScreenState.Main)
                {
                    if (_selectedIndex == 0)
                        StateManager.Replace(new OverworldState(Game, StateManager));
                    else if (_selectedIndex == 1)
                    {
                        _currentScreen = ScreenState.Settings;
                        _selectedIndex = 0;
                    }
                }
                else if (_currentScreen == ScreenState.Settings)
                {
                    if (_selectedIndex == 2) // Back option
                    {
                        _currentScreen = ScreenState.Main;
                        _selectedIndex = 1;
                    }
                }
            }
        }

        private void AdjustSetting(int direction)
        {
            if (_selectedIndex == 0) // Sound Volume
            {
                _volume = MathHelper.Clamp(_volume + (direction * 10), 0, 100);
                
                GameSettings.SetVolume(_volume);
            }
            else if (_selectedIndex == 1) // Target FPS
            {
                _fpsIndex = MathHelper.Clamp(_fpsIndex + direction, 0, _fpsOptions.Length - 1);
                int targetFps = _fpsOptions[_fpsIndex];
                
                GameSettings.SetTargetFps(Game, targetFps);
            }
        }
        
        public override void Draw(SpriteBatch spriteBatch)
        {
            Game.GraphicsDevice.Clear(BackgroundColor);

            spriteBatch.Begin(samplerState: SamplerState.PointClamp);

            int startX = 140; 
            int startY = 40;
            float textScale = 1.2f;
            
            string header = (_currentScreen == ScreenState.Main) ? "--- Instruction ---" : "--- Settings ---";
            Vector2 headerSize = Game.DialogueFont.MeasureString(header) * textScale;
            Vector2 headerPos  = new Vector2((GameSettings.WindowWidth - headerSize.X - 150) / 2f, startY);
            DrawText(spriteBatch, header, headerPos, HeaderColor, textScale);
            
            int lineY = startY + 50;

            if (_currentScreen == ScreenState.Main)
            {
                int lineHeight = 30;

                string[] instructions = new string[]
                {
                    "[Z or ENTER] - Confirm",
                    "[X] - Cancel",
                    "[C] - Menu (In-game)",
                    "[Hold ESC] - Quit",
                    "When HP is 0, you lose."
                };

                foreach (string line in instructions)
                {
                    DrawText(spriteBatch, line, new Vector2(startX, lineY), TextColor, textScale);
                    lineY += lineHeight;
                }

                lineY += 25;
                for (int i = 0; i < _menuOptions.Length; i++)
                {
                    bool isSelected = (i == _selectedIndex);
                    Color color = isSelected ? SelectedColor : TextColor;

                    DrawText(spriteBatch, _menuOptions[i], new Vector2(startX, lineY), color, textScale + 0.1f);
                    lineY += 36;
                }
            }
            
            else if (_currentScreen == ScreenState.Settings)
            {
                int lineHeight = 40;

                for (int i = 0; i < _settingsOptions.Length; i++)
                {
                    bool isSelected = (i == _selectedIndex);
                    Color color = isSelected ? SelectedColor : TextColor;
                    string optionText = _settingsOptions[i];
                    
                    if (i == 0) // Sound Volume
                    {
                        optionText = $"Sound Volume : < {_volume}% >";
                    }
                    else if (i == 1) // Target FPS
                    {
                        optionText = $"FPS Target   : < {_fpsOptions[_fpsIndex]} FPS >";
                    }

                    DrawText(spriteBatch, optionText, new Vector2(startX, lineY), color, textScale + 0.1f);
                    lineY += lineHeight;
                }

                // Draw helper key tip at bottom
                DrawText(spriteBatch, "[LEFT/RIGHT] Adjust    [X] Back", new Vector2(startX, lineY + 30), Color.DarkGray, 0.9f);
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
                scale * 2, // * 2 to increase size
                SpriteEffects.None,
                0f
            );
        }
    }
}