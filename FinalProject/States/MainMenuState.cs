using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using FinalProject.Core;
using FinalProject.Core.Audio;
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
        
        // real scales, already including the x2 the old helper hid. measuring and
        // drawing have to agree or centring silently goes wrong
        private const float BodyScale   = 2.4f;
        private const float OptionScale = 2.6f;
        private const float HintScale   = 1.8f;

        private const int LeftMargin = 140;
        private const int TopMargin  = 40;

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
                    SoundManager.Play(SoundManager.MenuSelect);
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
                
                SoundManager.Play(SoundManager.MenuMove);
            }

            // Cycle Down
            if (Game.Input.IsKeyPressed(Keys.Down))
            {
                _selectedIndex++;
                if (_selectedIndex >= optionCount)
                    _selectedIndex = 0;
                
                SoundManager.Play(SoundManager.MenuMove);
            }

            if (_currentScreen == ScreenState.Settings)
            {
                if (Game.Input.IsKeyPressed(Keys.Left))
                {
                    AdjustSetting(-1);
                    SoundManager.Play(SoundManager.MenuMove);
                }
                if (Game.Input.IsKeyPressed(Keys.Right))
                {
                    AdjustSetting(1);
                    SoundManager.Play(SoundManager.MenuMove);
                }
            }
            
            // Confirm Selection
            if (Game.Input.IsKeyPressed(Keys.Z) || Game.Input.IsKeyPressed(Keys.Enter))
            {
                SoundManager.Play(SoundManager.MenuSelect);
                
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
                    if (_selectedIndex == 1) // <--- THIS WAS MISSING
                    {
                        AdjustSetting(1);
                    }
                    else if (_selectedIndex == 2) // Back option
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
                _fpsIndex += direction;

                if (_fpsIndex < 0)
                    _fpsIndex = _fpsOptions.Length - 1;
                else if (_fpsIndex >= _fpsOptions.Length)
                    _fpsIndex = 0;

                int targetFps = _fpsOptions[_fpsIndex];
                GameSettings.SetTargetFps(Game, targetFps);
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

                    DrawText(spriteBatch, optionText, new Vector2(LeftMargin, lineY), color, OptionScale);
                    lineY += lineHeight;
                }

                // Draw helper key tip at bottom
                DrawText(spriteBatch, "[LEFT/RIGHT] Adjust    [X] Back",
                    new Vector2(LeftMargin, lineY + 30), Color.DarkGray, HintScale);
            }

            spriteBatch.End();
        }
        
        // draws at exactly the scale it's given — no hidden multiplier, so
        // MeasureString at the same scale gives the size that actually lands
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

        // horizontally centred on the window. only correct because DrawText no
        // longer scales behind our back — that mismatch is what the old "- 150"
        // was quietly compensating for
        private void DrawCentered(SpriteBatch spriteBatch, string text, float y, Color color, float scale)
        {
            float width = Game.DialogueFont.MeasureString(text).X * scale;
            DrawText(spriteBatch, text, new Vector2((GameSettings.WindowWidth - width) / 2f, y), color, scale);
        }
    }
}