// States/OverworldState.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
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
        private string  _currentAreaName;
        private List<MapTransition> _transitions  = new();
        private bool               _transitioning = false; // prevents repeated trigger on failed load

        // ── Lifecycle ────────────────────────────────────────────────────────

        public override void OnEnter()
        {
            _player = new Player(Game, 5, 5);
            _camera = new Camera();
            LoadMap("town_1", 5, 5);
        }

        public override void Update(GameTime gameTime)
        {
            if (_map == null) return;

            _player.Update(gameTime, _map);
            _camera.Follow(_player, _map);
            CheckTransitions();

            // Debug: press F1 to log the player's current tile position
            if (Game.Input.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.F1))
                LogDebug($"Player tile: ({_player.TilePosition.X}, {_player.TilePosition.Y}) on {_currentAreaName}");
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            Game.GraphicsDevice.Clear(BackgroundColor);

            spriteBatch.Begin(
                samplerState: SamplerState.PointClamp,
                transformMatrix: _camera.GetTransform()
            );
            _map?.Draw(spriteBatch);
            _player.Draw(spriteBatch);
            spriteBatch.End();
        }

        // ── Transitions ──────────────────────────────────────────────────────

        private void CheckTransitions()
        {
            if (_player.IsMoving || _transitioning) return;

            foreach (MapTransition t in _transitions)
            {
                Point next = StepInDirection(_player.TilePosition, t.Direction);

                // The player must be facing the exit and their next step would leave the map
                bool facingExit  = _player.Facing.ToString() == t.Direction;
                bool leavingMap  = !_map.IsInBounds(next.X, next.Y);

                // For Up/Down exits the opening is a column range; for Left/Right a row range
                bool inRange = t.Direction is "Up" or "Down"
                    ? _player.TilePosition.X >= t.TileMin && _player.TilePosition.X <= t.TileMax
                    : _player.TilePosition.Y >= t.TileMin && _player.TilePosition.Y <= t.TileMax;

                if (facingExit && leavingMap && inRange)
                {
                    _transitioning = true;
                    LoadMap(t.TargetMap, t.SpawnX, t.SpawnY);
                    _transitioning = false;
                    return;
                }
            }
        }

        private static Point StepInDirection(Point from, string direction) => direction switch
        {
            "Up"    => new Point(from.X,     from.Y - 1),
            "Down"  => new Point(from.X,     from.Y + 1),
            "Left"  => new Point(from.X - 1, from.Y),
            "Right" => new Point(from.X + 1, from.Y),
            _       => from
        };

        // ── Map loading ──────────────────────────────────────────────────────

        private void LoadMap(string mapName, int spawnX, int spawnY)
        {
            _currentAreaName = mapName;

            string mapsDir = Path.GetFullPath(
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Content", "Maps"));

            string path = Path.Combine(mapsDir, mapName + ".tmj");
            if (!File.Exists(path))
                path = Path.Combine(mapsDir, mapName + ".json");

            try
            {
                _map = MapLoader.Load(path, Game.Content);
            }
            catch (Exception e)
            {
                LogDebug($"MAP LOAD ERROR ({mapName}): {e.Message}\n{e.StackTrace}");
                return;
            }

            _player.Teleport(spawnX, spawnY);
            _transitions = LoadTransitions(mapsDir, mapName);

            EventBus.Instance.Publish(new AreaChangedEvent(_currentAreaName));
        }

        private static List<MapTransition> LoadTransitions(string mapsDir, string mapName)
        {
            string path = Path.Combine(mapsDir, mapName + ".transitions.json");
            if (!File.Exists(path)) return new List<MapTransition>();

            try
            {
                string json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<List<MapTransition>>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    ?? new List<MapTransition>();
            }
            catch { return new List<MapTransition>(); }
        }

        private static void LogDebug(string message)
        {
            try
            {
                File.AppendAllText(
                    Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "map_debug.txt")),
                    message + "\n");
            }
            catch { }
        }
    }
}
