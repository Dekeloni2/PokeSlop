// Game1.cs
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

        // Shared font for dialogue/UI text (Content/Fonts/DialogueFont.spritefont).
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

<<<<<<< Updated upstream
            // Load data registries from JSON so new content never requires a recompile
            string dataDir = Path.GetFullPath(
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Content", "Data"));
            MoveRegistry.Load(Path.Combine(dataDir, "moves.json"));
            SpeciesRegistry.Load(Path.Combine(dataDir, "species.json"));
=======
            DialogueFont = Content.Load<SpriteFont>("Fonts/DialogueFont");

            // NOTE: this used to load Content/Data/moves.json and species.json
            // into a MoveRegistry/SpeciesRegistry (Pokemon-style creature data).
            // Both registries and that Pokemon-shaped JSON belong to the old
            // creature-collecting design and don't apply to the Teacher-boss
            // system — removed rather than adapted. Boss Teachers are built
            // directly from TeacherStats (see Data/TeacherStats.cs) wherever a
            // fight is triggered; there's no data-driven loading step for them
            // yet.
>>>>>>> Stashed changes

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
