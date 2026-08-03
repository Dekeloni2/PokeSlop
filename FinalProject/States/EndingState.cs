using FinalProject.Core.StateMachine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using FinalProject.Core;
using FinalProject.Events;

namespace FinalProject.States;

public class EndingState : GameState
{
    private readonly Game1 _game;
    private readonly GameStateManager _stateManager;
    
    private string _endingTitle = ""; // good bad or however u want to name them 
    private string _endingText = ""; 
    
    public EndingState(Game1 game, GameStateManager stateManager) : base(game, stateManager)
    {
        _game = game;
        _stateManager = stateManager;
    }

    public override void OnEnter()
    {
        bool yakirKilled = _game.Route.OutcomeFor("yakir") == BattleOutcome.Killed;
        bool dorbendorKilled = _game.Route.OutcomeFor("dorbendor") == BattleOutcome.Killed;
        bool davidKilled = _game.Route.OutcomeFor("david") == BattleOutcome.Killed;

        int totalKills = _game.Route.Killed;
        
        // --- 1. All Spared (Pacifist) ---
        if (totalKills == 0)
        {
            _endingTitle = "PACIFIST ENDING";
            _endingText = "You passed all of your tests successfully.\n" +
                          "No teachers were killed!\n" +
                          "You found massive success in your Game Dev career!\n" +
                          "You eventually founded your own indie studio and made your dream game.\n" +
                          "It was critically acclaimed, winning Game of the Year\n" +
                          "at The Game Awards hosted by Geoff Keighley!";
        }
        
        // --- 2. All killed (Genocide) ---
        else if (yakirKilled && dorbendorKilled && davidKilled)
        {
            _endingTitle = "GENOCIDE ENDING";
            _endingText = "You showed no mercy.\n" +
                          "All teachers were killed and Tiltan was closed down.\n" +
                          "The police came to arrest you.\n" +
                          "Despite putting up your best fight, you were overwhelmed and captured.\n" +
                          "You were charged with mass manslaughter...\n" +
                          "Enjoy rotting in jail!";
        }
        
        // --- 3. Only Yakir Killed ---
        else if (yakirKilled && !dorbendorKilled && !davidKilled)
        {
            _endingTitle = "Only Yakir Killed";
            _endingText = "";
        }
        
        // --- 4. Only Dor Killed ---
        else if (!yakirKilled && dorbendorKilled && !davidKilled)
        {
            _endingTitle = "Only Dor Killed";
            _endingText = "";
        }
        
        // --- 5. Only David Killed ---
        else if (!yakirKilled && !dorbendorKilled && davidKilled)
        {
            _endingTitle = "Only David Killed";
            _endingText = "";
        }
        
        // --- 6. Yakir & Dor Killed (Only David Spared) ---
        else if (yakirKilled && dorbendorKilled && !davidKilled)
        {
            _endingTitle = "Only David Spared";
            _endingText = "";
        }
        // --- 7. Yakir & David Killed (Only Dor Spared) ---
        else if (yakirKilled && !dorbendorKilled && davidKilled)
        {
            _endingTitle = "Only Dor Spared";
            _endingText = "";
        }
        // --- 8. Dor & David Killed (Only Yakir Spared) ---
        else if (!yakirKilled && dorbendorKilled && davidKilled)
        {
            _endingTitle = "Only Yakir Spared";
            _endingText = "";
        }
    }

    public override void Update(GameTime gameTime)
    {
        // Press Z or Enter to return to Title Screen
        if (_game.Input.IsKeyPressed(Keys.Z) || _game.Input.IsKeyPressed(Keys.Enter))
        {
            // Reset tracker if starting fresh on main menu
            _game.Route.Reset();
            _stateManager.Replace(new MainMenuState(_game, _stateManager));
        }
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Begin();

        // Title
        Vector2 titlePos = new Vector2(50, 60);
        spriteBatch.DrawString(_game.DialogueFont, _endingTitle, titlePos, Color.Yellow, 0f, Vector2.Zero, 3f, SpriteEffects.None, 0f);

        // Ending Story Text
        Vector2 textPos = new Vector2(50, 140);
        spriteBatch.DrawString(_game.DialogueFont, _endingText, textPos, Color.White, 0f, Vector2.Zero, 1.7f, SpriteEffects.None, 0f);

        // Continue
        string prompt = "[Press Z or ENTER to return to Main Menu]";
        Vector2 promptPos = new Vector2(50, GameSettings.WindowHeight - 50);
        spriteBatch.DrawString(_game.DialogueFont, prompt, promptPos, Color.Gray, 0f, Vector2.Zero, 1.9f, SpriteEffects.None, 0f);

        spriteBatch.End();
    }
}