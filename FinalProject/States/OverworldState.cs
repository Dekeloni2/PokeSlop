// States/OverworldState.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using FinalProject.Battle;
using FinalProject.Core;
using FinalProject.Data;
using FinalProject.Entities;
using FinalProject.World;
using FinalProject.Events;
using FinalProject.UI;

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

        // Undertale-style textbox — owns input while open (see Update below).
        private DialogueBox _dialogueBox;
        // Cache of already-loaded maps so backtracking doesn't re-parse JSON from disk
        private readonly Dictionary<string, TileMap> _mapCache = new();

        // ── Lifecycle ────────────────────────────────────────────────────────

        public override void OnEnter()
        {
            _player      = new Player(Game, 6, 14);
            _camera      = new Camera();
            _dialogueBox = new DialogueBox(Game.PixelTexture, Game.DialogueFont);
            // TODO: swap back to the real starting map once the tileset rework
            // lands — pointed at "entrance" for now to test the Interactables layer.
            LoadMap("entrance", 6, 14);
        }

        public override void Update(GameTime gameTime)
        {
            // The dialogue box owns input while a conversation is on screen —
            // updated (and testable) independent of whether the map loaded, so
            // it isn't blocked by the in-progress tileset rework.
            if (_dialogueBox.IsActive)
            {
                _dialogueBox.Update(gameTime, Game.Input);
                return;
            }

            if (Game.Input.IsKeyPressed(Keys.Z))
            {
                TryInteract();
                return;
            }

#if DEBUG
            // debug builds only - press B to start a test battle, remove once
            // real encounter triggers exist
            if (Game.Input.IsKeyPressed(Keys.B))
            {
                StartDebugBattle();
                return;
            }
#endif

            if (_map == null) return;

            _player.Update(gameTime, _map);
            _camera.Follow(_player, _map);

            // NOTE: there used to be a wild-encounter check here (random
            // creature battles in tall grass). This is an Undertale-style
            // game — Teachers are specific bosses, not a random wild-catch
            // pool — so that's gone. However a boss battle actually starts is
            // still TBD (probably via TryInteract below, walking up to a
            // Teacher NPC, but that hookup doesn't exist yet).

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

            // UI layer — screen space, unaffected by the world camera's zoom/scroll.
            spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _dialogueBox.Draw(spriteBatch);
            spriteBatch.End();
        }

        // opens the dialogue box if the tile the player is facing has an
        // interactable on it. Teacher encounters will probably hook in here too
        private void TryInteract()
        {
            if (_map == null) return;

            Point facingTile = _player.Facing.GetNeighbour(_player.TilePosition);
            Interactable interactable = _map.GetInteractableAt(facingTile.X, facingTile.Y);

            if (interactable != null)
                _dialogueBox.Open(interactable.Text);
        }

#if DEBUG
        // builds a test teacher and starts a battle. Push (not Replace) so
        // this state resumes when the battle pops itself
        private void StartDebugBattle()
        {
            string teachersDir = Path.GetFullPath(
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Content", "Teachers"));
            TeacherStats stats = TeacherLoader.Load(Path.Combine(teachersDir, "substitute.json"));
            var teacher = new Teacher(stats);

            StateManager.Push(new BattleState(Game, StateManager, teacher));
        }
#endif

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
