// Core/GameState.cs
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FinalProject.Core
{
    public abstract class GameState
    {
        protected Game1            Game         { get; }
        protected GameStateManager StateManager { get; }

        protected GameState(Game1 game, GameStateManager stateManager)
        {
            Game         = game;
            StateManager = stateManager;
        }

        public virtual void OnEnter() { }
        public virtual void Pause()   { }
        public virtual void Resume()  { }
        public virtual void OnExit()  { }

        public abstract void Update(GameTime gameTime);
        public abstract void Draw(SpriteBatch spriteBatch);
    }
}