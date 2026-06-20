// States/OverworldState.cs
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FinalProject.Core;
using FinalProject.Entities;
using FinalProject.World;
using FinalProject.Events;

namespace FinalProject.States
{
    public class OverworldState(Game1 game, GameStateManager stateManager) : GameState(game, stateManager)
    {
        private static readonly Color BackgroundColor = new Color(20, 120, 20);
        private Player _player;
        private Camera _camera;
        private TileMap _map;
        private string _currentAreaName = "town_1";

        public override void OnEnter()
        {
            // Try loading JSON first, then fall back to .tmj (Tiled map editor export)
            string jsonPath = "Content/Maps/town_1.json";
            string tmjPath  = "Content/Maps/town_1.tmj";

            try
            {
                if (System.IO.File.Exists(jsonPath))
                    _map = MapLoader.Load(jsonPath, Game.Content);
                else if (System.IO.File.Exists(tmjPath))
                    _map = MapLoader.Load(tmjPath, Game.Content);

                if (_map != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Loaded map: {_currentAreaName} ({_map.Width}x{_map.Height} tiles)");
                    Console.WriteLine($"Loaded map: {_currentAreaName} ({_map.Width}x{_map.Height} tiles)");
                }
                else
                    throw new System.IO.FileNotFoundException("No map file found for town_1");

                _player = new Player(Game, 5, 5);
                _camera = new Camera();
                EventBus.Instance.Publish(new AreaChangedEvent(_currentAreaName));
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("MAP LOAD ERROR: " + e.Message);
                System.Diagnostics.Debug.WriteLine(e.StackTrace);
                // If map load fails, ensure player and camera still exist so the overworld runs
                if (_player == null) _player = new Player(Game, 5, 5);
                if (_camera == null) _camera = new Camera();
            }
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

            // Draw the map (if loaded) first, then the player and other entities
            _map?.Draw(spriteBatch);
            _player.Draw(spriteBatch);
            // TODO: Draw NPCs, HUD

            spriteBatch.End();
        }
    }
}

