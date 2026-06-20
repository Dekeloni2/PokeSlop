// States/OverworldState.cs
using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FinalProject.Core;
using FinalProject.Entities;
using FinalProject.World;
using FinalProject.Events;

namespace FinalProject.States
{
    public class OverworldState : GameState
    {
        public OverworldState(Game1 game, GameStateManager stateManager)
            : base(game, stateManager) { }
        private static readonly Color BackgroundColor = new Color(20, 120, 20);
        private Player _player;
        private Camera _camera;
        private TileMap _map;
        private string _currentAreaName = "town_1";

        public override void OnEnter()
        {
            try { File.AppendAllText(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "map_debug.txt")), "OverworldState.OnEnter called\n"); } catch { }
            // Try loading JSON first, then fall back to .tmj (Tiled map editor export)
            // Resolve paths relative to the compiled binary so the files are found
            // whether running from the project root or the build output directory.
            string jsonPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Content", "Maps", "town_1.json"));
            string tmjPath  = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Content", "Maps", "town_1.tmj"));

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
                    try
                    {
                        string logPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "map_debug.txt"));
                        File.AppendAllText(logPath, $"Loaded map: {_currentAreaName} ({_map.Width}x{_map.Height})\n");
                    }
                    catch { }
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
                try
                {
                    string logPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "map_debug.txt"));
                    File.AppendAllText(logPath, $"MAP LOAD ERROR: {e.Message}\n{e.StackTrace}\n");
                }
                catch { }
                // If map load fails, ensure player and camera still exist so the overworld runs
                if (_player == null) _player = new Player(Game, 5, 5);
                if (_camera == null) _camera = new Camera();
            }
        }

        public override void Update(GameTime gameTime)
        {
            // Guard: if map failed to load, nothing should move
            if (_map == null) return;

            _player.Update(gameTime, _map);
            _camera.Follow(_player, _map);
            // TODO: Handle world state, check for encounters
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            Game.GraphicsDevice.Clear(BackgroundColor);
            // PointClamp = nearest-neighbour sampling, no bleeding between tile edges
            spriteBatch.Begin(
                samplerState: Microsoft.Xna.Framework.Graphics.SamplerState.PointClamp,
                transformMatrix: _camera.GetTransform()
            );

            // Draw the map (if loaded) first, then the player and other entities
            _map?.Draw(spriteBatch);
            _player.Draw(spriteBatch);
            // TODO: Draw NPCs, HUD

            // Draw a visible map boundary to help debug when the tiles appear off-screen
            if (_map != null)
            {
                Rectangle mapRect = new Rectangle(0, 0, _map.PixelWidth, _map.PixelHeight);
                DrawRectangleOutline(spriteBatch, mapRect, 2, Color.Red * 0.6f);
            }

            spriteBatch.End();
        }

        // Helper to draw a rectangle outline using the 1x1 PixelTexture from Game.
        private void DrawRectangleOutline(SpriteBatch spriteBatch, Rectangle rect, int thickness, Color color)
        {
            // Top
            spriteBatch.Draw(Game.PixelTexture, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
            // Bottom
            spriteBatch.Draw(Game.PixelTexture, new Rectangle(rect.X, rect.Y + rect.Height - thickness, rect.Width, thickness), color);
            // Left
            spriteBatch.Draw(Game.PixelTexture, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
            // Right
            spriteBatch.Draw(Game.PixelTexture, new Rectangle(rect.X + rect.Width - thickness, rect.Y, thickness, rect.Height), color);
        }
    }
}

