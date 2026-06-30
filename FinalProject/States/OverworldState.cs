// States/OverworldState.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using FinalProject.Battle;
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
        private bool _transitioning = false; // prevents repeated trigger on failed load
        private EncounterTable _encounterTable;
        private bool           _wasMoving = false;
        private readonly Random _rng      = new();
        
        // Cache of already-loaded maps so backtracking doesn't re-parse JSON from disk
        private readonly Dictionary<string, TileMap> _mapCache = new();

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

            _wasMoving = _player.IsMoving;
            _player.Update(gameTime, _map);
            _camera.Follow(_player, _map);

            // Check for wild encounter the moment a step completes
            if (_wasMoving && !_player.IsMoving)
                CheckWildEncounter();

            CheckTransitions();
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            Game.GraphicsDevice.Clear(BackgroundColor);

            spriteBatch.Begin(
                samplerState: SamplerState.PointClamp,
                transformMatrix: _camera.GetTransform()
            );
            _map?.Draw(spriteBatch, _camera);
            _player.Draw(spriteBatch);
            spriteBatch.End();
        }
        
        private void CheckWildEncounter()
        {
            if (_encounterTable == null) return;
            if (!_map.IsTallGrass(_player.TilePosition.X, _player.TilePosition.Y)) return;
            if (_rng.NextDouble() >= GameSettings.WildEncounterChance) return;

            Creature wild = _encounterTable.SpawnRandom(_rng);
            StateManager.Push(new BattleState(Game, StateManager, wild));
        }

        // ── Transitions ──────────────────────────────────────────────────────

        private void CheckTransitions()
        {
            if (_player.IsMoving || _transitioning) return;

            foreach (MapTransition t in _transitions)
            {
                Point next = t.Direction.GetNeighbour(_player.TilePosition);

                // The player must be facing the exit and their next step would leave the map
                bool facingExit = _player.Facing == t.Direction;
                bool leavingMap = !_map.IsInBounds(next.X, next.Y);

                // For Up/Down exits the opening is a column range; for Left/Right a row range
                bool inRange = t.Direction is Direction.Up or Direction.Down
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

        // ── Map loading ──────────────────────────────────────────────────────

        private void LoadMap(string mapName, int spawnX, int spawnY)
        {
            _currentAreaName = mapName;

            string mapsDir = Path.GetFullPath(
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Content", "Maps"));

            // Return cached map if we've already loaded it
            if (!_mapCache.TryGetValue(mapName, out TileMap loaded))
            {
                string path = Path.Combine(mapsDir, mapName + ".tmj");
                if (!File.Exists(path))
                    path = Path.Combine(mapsDir, mapName + ".json");

                try
                {
                    loaded = MapLoader.Load(path, Game.Content);
                    _mapCache[mapName] = loaded;
                }
                catch (Exception e)
                {
                    LogDebug($"MAP LOAD ERROR ({mapName}): {e.Message}\n{e.StackTrace}");
                    return;
                }
            }

            _map = loaded;
            _player.Teleport(spawnX, spawnY);
            _transitions    = LoadTransitions(mapsDir, mapName);
            _encounterTable = LoadEncounterTable(mapsDir, mapName);

            EventBus.Instance.Publish(new AreaChangedEvent(_currentAreaName));
        }

        private static List<MapTransition> LoadTransitions(string mapsDir, string mapName)
        {
            string path = Path.Combine(mapsDir, mapName + ".transitions.json");
            if (!File.Exists(path)) return new List<MapTransition>();

            try
            {
                string json = File.ReadAllText(path);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
                };
                return JsonSerializer.Deserialize<List<MapTransition>>(json, options)
                    ?? new List<MapTransition>();
            }
            catch { return new List<MapTransition>(); }
        }

        private static EncounterTable LoadEncounterTable(string mapsDir, string mapName)
        {
            string path = Path.Combine(mapsDir, mapName + ".encounters.json");
            if (!File.Exists(path)) return null;

            try
            {
                string json = File.ReadAllText(path);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var entries = JsonSerializer.Deserialize<List<EncounterEntry>>(json, options);
                return entries != null && entries.Count > 0 ? new EncounterTable(entries) : null;
            }
            catch { return null; }
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
