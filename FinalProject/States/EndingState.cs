using FinalProject.Core.StateMachine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using FinalProject.Core;

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
        Route currentRoute = _game.Route.Current;
        int killedCount = _game.Route.Killed;
        int sparedCount = _game.Route.Spared;

        switch (currentRoute)
        {
            case Route.Pacifist:
                _endingTitle = "PACIFIST ENDING";
                _endingText = "You passed all of your tests successfully.\n" +
                              "No teachers were killed!\n" +
                              "You found massive success in your Game Dev career!\n" +
                              "You eventually founded your own indie studio and made your dream game.\n" +
                              "It was critically acclaimed, winning Game of the Year\n" +
                              "at The Game Awards hosted by Geoff Keighley!";
                break;

            case Route.Genocide:
                _endingTitle = "GENOCIDE ENDING";
                _endingText = "You showed no mercy.\n" +
                              "All teachers were killed and Tiltan was closed down.\n" +
                              "The police came to arrest you.\n" +
                              "Despite putting up your best fight, you were overwhelmed and captured.\n" +
                              "You were charged with mass manslaughter...\n" +
                              "Enjoy rotting in jail!";
                break;

            case Route.Neutral:
            default:
                _endingTitle = "NEUTRAL ENDING";
                _endingText = $"You passed the semester, but killed {killedCount} teacher(s).\n" +
                              "You were eventually arrested for manslaughter...\n\n" +
                              "After serving your sentence, you found a job at a mobile game company\n" +
                              "making the absolute hottest slop on the market.";
                break;
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
        spriteBatch.DrawString(_game.DialogueFont, _endingTitle, titlePos, Color.Yellow, 0f, Vector2.Zero, 1.5f, SpriteEffects.None, 0f);

        // Ending Story Text
        Vector2 textPos = new Vector2(50, 140);
        spriteBatch.DrawString(_game.DialogueFont, _endingText, textPos, Color.White, 0f, Vector2.Zero, 1.2f, SpriteEffects.None, 0f);

        // Continue
        string prompt = "[Press Z or ENTER to return to Main Menu]";
        Vector2 promptPos = new Vector2(50, GameSettings.WindowHeight - 50);
        spriteBatch.DrawString(_game.DialogueFont, prompt, promptPos, Color.Gray, 0f, Vector2.Zero, 1.0f, SpriteEffects.None, 0f);

        spriteBatch.End();
    }
}