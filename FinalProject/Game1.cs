// Game1.cs
using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using FinalProject.Core;
using FinalProject.Core.Graphics;
using FinalProject.Core.Input;
using FinalProject.Core.StateMachine;
using FinalProject.Data;
using FinalProject.States;

namespace FinalProject
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        // shared systems, public so states can reach them through Game.X
        public GameStateManager StateManager { get; private set; }
        public InputManager     Input        { get; private set; }

        // 1x1 white pixel for drawing plain rectangles (HP bars, borders...)
        public Texture2D PixelTexture { get; private set; }

        public SpriteFont DialogueFont { get; private set; }

        public PlayerData PlayerData { get; private set; } = new();

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this)
            {
                PreferredBackBufferWidth  = GameSettings.WindowWidth,
                PreferredBackBufferHeight = GameSettings.WindowHeight
            };

            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            Window.Title = GameSettings.GameTitle;

            Input        = new InputManager();
            StateManager = new GameStateManager();

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            PixelTexture = new Texture2D(GraphicsDevice, 1, 1);
            PixelTexture.SetData(new[] { Color.White });

            DialogueFont = Content.Load<SpriteFont>("Fonts/DialogueFont");
            
            new SpriteManager(Content);
            SpriteManager.AddSprite("student_world", "Sprites/Player/student_world", 4, 3);
            SpriteManager.AddSprite("soul", "Sprites/Battle/Soul", 2, 1);
            SpriteManager.AddSprite("battleButtons", "Sprites/Battle/buttons", 2, 4, 10, 8);
            SpriteManager.AddSprite("attackZone", "Sprites/Battle/attack_minigame");
            SpriteManager.AddSprite("attackBar", "Sprites/Battle/attack_target", 2, 1, 6);
            SpriteManager.AddSprite("boat", "Sprites/AttackPatterns/boat");
            SpriteManager.AddSprite("garlicGun", "Sprites/AttackPatterns/GarlicGun");
            SpriteManager.AddSprite("vegeta", "Sprites/AttackPatterns/Vegeta",5 , 1);
            SpriteManager.AddSprite("warning", "Sprites/Battle/warning", 3, 1);

#if DEBUG
            LoadStartingInventory();
#endif

            StateManager.Replace(new MainMenuState(this, StateManager));
        }

        protected override void Update(GameTime gameTime)
        {
            // input first so every state sees the same snapshot this frame
            Input.Update();

            if (Input.IsKeyPressed(Keys.Escape))
                Exit();

            StateManager.Update(gameTime);
            base.Update(gameTime);
        }

#if DEBUG
        // debug builds only - gives the player one of every item so ITEM can
        // be tested until there's a real way to get items (shop/pickups)
        private void LoadStartingInventory()
        {
            string itemsPath = Path.GetFullPath(
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Content", "Items", "items.json"));
            PlayerData.Inventory.AddRange(ItemLoader.LoadAll(itemsPath));
        }
#endif

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);
            StateManager.Draw(_spriteBatch);
            base.Draw(gameTime);
        }
    }
}
