// Game1.cs
using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using FinalProject.Core;
using FinalProject.Data;
using FinalProject.States;

namespace FinalProject
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        // Shared systems — public so any state can reach them through Game.X
        public GameStateManager StateManager { get; private set; }
        public InputManager     Input        { get; private set; }

        // A 1x1 white pixel, tinted and stretched to draw any colored rectangle.
        // Useful for HP bars, overlays, debug rects — no dedicated sprite needed.
        public Texture2D PixelTexture { get; private set; }

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

            // Load data registries from JSON so new content never requires a recompile
            string dataDir = Path.GetFullPath(
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Content", "Data"));
            MoveRegistry.Load(Path.Combine(dataDir, "moves.json"));
            SpeciesRegistry.Load(Path.Combine(dataDir, "species.json"));

            // First thing the player sees
            StateManager.Replace(new MainMenuState(this, StateManager));
        }

        protected override void Update(GameTime gameTime)
        {
            // Input first so every state sees a consistent snapshot this frame
            Input.Update();

            if (Input.IsKeyPressed(Keys.Escape))
                Exit();

            StateManager.Update(gameTime);
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);
            StateManager.Draw(_spriteBatch);
            base.Draw(gameTime);
        }
    }
}