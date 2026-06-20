// States/OverworldState.cs
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FinalProject.Core;
using FinalProject.Entities;

namespace FinalProject.States
{
    public class OverworldState : GameState
    {
        private static readonly Color BackgroundColor = new Color(20, 120, 20);
        private Player _player;
        private Camera _camera;

        public OverworldState(Game1 game, GameStateManager stateManager)
            : base(game, stateManager) { }

        public override void OnEnter()
        {
            _player = new Player(Game, 5, 5);
            _camera = new Camera();
            // TODO: Load world data, player position, NPCs, etc.
        }

        public override void Update(GameTime gameTime)
        {
            _player.Update(gameTime);
            _camera.Follow(_player);
            // TODO: Handle world state, check for encounters
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            Game.GraphicsDevice.Clear(BackgroundColor);
            
            spriteBatch.Begin(transformMatrix: _camera.GetTransform());
            
            _player.Draw(spriteBatch);
            // TODO: Draw tilemap, NPCs, HUD
            
            spriteBatch.End();
        }
    }
}

