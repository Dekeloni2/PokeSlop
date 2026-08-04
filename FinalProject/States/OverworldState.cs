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
using FinalProject.Core.Audio;
using FinalProject.Core.Graphics;
using FinalProject.Core.StateMachine;
using FinalProject.Data;
using FinalProject.Entities;
using FinalProject.World;
using FinalProject.Events;
using FinalProject.UI;

namespace FinalProject.States
{
    public class OverworldState : GameState, ElevatorSequence.IElevatorHost
    {
        public OverworldState(Game1 game, GameStateManager stateManager)
            : base(game, stateManager) { }

        private static readonly Color BackgroundColor = Color.Black;

        private Player _player;
        private Camera _camera;
        private TileMap _map;
        private string  _currentAreaName;
        private List<MapTransition> _transitions  = new();
        private List<MapWarp>       _warps        = new();
        private bool _transitioning = false; // prevents repeated trigger on failed load

        // Undertale-style textbox — owns input while open (see Update below).
        private DialogueBox _dialogueBox;
        // player menu
        private OverworldMenu _menu;
        // Cache of already-loaded maps so backtracking doesn't re-parse JSON from disk
        private readonly Dictionary<string, TileMap> _mapCache = new();

        // ── Lifecycle ────────────────────────────────────────────────────────

        public override void OnEnter()
        {
            _player      = new Player(Game, 6, 14);
            _camera      = new Camera();
            _dialogueBox = new DialogueBox(Game.PixelTexture, Game.DialogueFont);
            _menu        = new OverworldMenu(Game.PixelTexture, Game.DialogueFont);
            _elevator    = new ElevatorSequence(Game.PixelTexture, Game.DialogueFont);
            // TODO: swap back to the real starting map once the tileset rework
            // lands — pointed at "entrance" for now to test the Interactables layer.
            LoadMap("entrance", 6, 14);
            
            _dialogueBox = new DialogueBox(Game.PixelTexture, Game.DialogueFont);
            _menu        = new OverworldMenu(Game.PixelTexture, Game.DialogueFont);
        }

        // comes back up from black when a battle pops off the stack, picking up
        // where the battle's fade out left it
        private const float FadeInSeconds  = 0.3f;
        private const float FadeOutSeconds = 0.3f;
        private float _fadeInLeft;

        // map changes fade out, swap, then fade back in, same as leaving a battle
        private float _fadeOut; // 0..1 while going to black
        private (string Map, int X, int Y) _pendingLoad;
        private bool _hasPendingLoad;

        // everything that changes map goes through here so they all get the fade
        private void BeginMapChange(string map, int spawnX, int spawnY)
        {
            if (_hasPendingLoad) return;

            _pendingLoad    = (map, spawnX, spawnY);
            _hasPendingLoad = true;
            _transitioning  = true;
            _fadeOut        = 0f;
        }

        // ── elevator ─────────────────────────────────────────────────────────
        // the lift runs itself (see ElevatorSequence), this state just gives it
        // the few things it needs through IElevatorHost, below
        private ElevatorSequence _elevator;
        private bool _awaitingFloorMenu; // prompt is up, floors come after it

        bool ElevatorSequence.IElevatorHost.IsChangingMap => _transitioning;

        bool ElevatorSequence.IElevatorHost.PlayerOnDoorTile
            => _map != null && _map.IsElevatorDoorTile(_player.TilePosition.X, _player.TilePosition.Y);

        void ElevatorSequence.IElevatorHost.FacePlayerOut() => _player.Face(Direction.Down);

        void ElevatorSequence.IElevatorHost.SetDoorsShut(bool shut)
        {
            if (_map != null) _map.ElevatorDoorVisible = shut;
        }

        void ElevatorSequence.IElevatorHost.ChangeMap(string map, int spawnX, int spawnY)
            => BeginMapChange(map, spawnX, spawnY);

        public override void Resume()
        {
            _fadeInLeft = FadeInSeconds;
            RefreshNpcs(); // an NPC's fight may have just been resolved

            // a battle stops the music on its way out, so the area's own track
            // has to be started again from scratch rather than left alone
            UpdateAreaMusic(force: true);
        }

        // ── Music ────────────────────────────────────────────────────────────

        // background track per map, keyed by the name LoadMap was given. maps
        // that aren't listed play nothing — the lift is meant to be quiet, and
        // it runs its own track while it's moving
        private static readonly Dictionary<string, string> AreaMusic = new()
        {
            ["tiltan_hall"] = "overworld",
        };

        // whatever this area asked for last, so walking between two maps that
        // share a track doesn't restart it from the top
        private string _areaSong;

        private void UpdateAreaMusic(bool force = false)
        {
            AreaMusic.TryGetValue(_currentAreaName ?? "", out string song);

            if (!force && song == _areaSong) return;

            _areaSong = song;

            if (song == null) SoundManager.StopMusic();
            else              SoundManager.PlayMusic(song);
        }

        public override void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (_fadeInLeft > 0f) _fadeInLeft -= dt;

            // holding still while the screen goes black, then swap and come back
            if (_hasPendingLoad)
            {
                _fadeOut += dt / FadeOutSeconds;
                if (_fadeOut < 1f) return;

                (string map, int x, int y) = _pendingLoad;
                _hasPendingLoad = false;
                _transitioning  = false;
                _fadeOut        = 0f;

                LoadMap(map, x, y);
                _fadeInLeft = FadeInSeconds;
                return;
            }

            // riding or picking a floor, either way the player can't move
            if (_elevator.IsBusy)
            {
                _elevator.Update(gameTime, Game.Input, this);
                return;
            }

            // The dialogue box owns input while a conversation is on screen —
            // updated (and testable) independent of whether the map loaded, so
            // it isn't blocked by the in-progress tileset rework.
            if (_dialogueBox.IsActive)
            {
                _dialogueBox.Update(gameTime, Game.Input);
                return;
            }
            
            // Menu owns input when active ─
            if (_menu.IsActive)
            {
                _menu.Update(Game.Input, Game.PlayerData, (message) =>
                {
                    _menu.Close();
                    _dialogueBox.Open(message);
                });
                return;
            }

            // Open Menu when C is pressed ─
            if (Game.Input.IsKeyPressed(Keys.C))
            {
                SoundManager.Play(SoundManager.MenuSelect);
                _menu.Open();
                return;
            }

            // the "select a location" prompt just closed, bring up the floors
            if (_awaitingFloorMenu)
            {
                _awaitingFloorMenu = false;
                _elevator.OpenFloorMenu();
                return;
            }

            if (Game.Input.IsKeyPressed(Keys.Z) || Game.Input.IsKeyPressed(Keys.Enter))
            {
                TryInteract();
                return;
            }
            
            if (_dialogueBox.IsActive)
            {
                _dialogueBox.Update(gameTime, Game.Input);
                return;
            }

            if (_menu.IsActive)
            {
                _menu.Update(Game.Input, Game.PlayerData, (message) =>
                {
                    _menu.Close();
                    _dialogueBox.Open(message);
                });
                return;
            }

            if (Game.Input.IsKeyPressed(Keys.C) || Game.Input.IsKeyPressed(Keys.LeftControl))
            {
                _menu.Open();
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

            // debug builds only - press O to open/close the elevator door layer
            // (no effect on maps without an ElevatorDoor layer)
            if (_map != null && _map.HasElevatorDoor && Game.Input.IsKeyPressed(Keys.O))
            {
                _map.ElevatorDoorVisible = !_map.ElevatorDoorVisible;
                return;
            }
#endif

            if (_map == null) return;

            _player.Update(gameTime, _map);
            _camera.Follow(_player, _map);

            if (CheckNpcBump()) return; // battle just started, this state is paused

            CheckTransitions();
            CheckWarps();
            _elevator.CheckExit(this);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            Game.GraphicsDevice.Clear(BackgroundColor);

            // the lift judders on the way between floors. applied after the
            // camera so it shakes the view rather than the world, and it's
            // Vector2.Zero whenever the lift isn't moving
            Matrix view = _camera.GetTransform();
            Vector2 judder = _elevator.ShakeOffset;
            if (judder != Vector2.Zero)
                view *= Matrix.CreateTranslation(judder.X, judder.Y, 0f);

            spriteBatch.Begin(
                samplerState: SamplerState.PointClamp,
                transformMatrix: view
            );
            _map?.Draw(spriteBatch, _camera);
            DrawNpcs(spriteBatch);
            _player.Draw(spriteBatch);
            spriteBatch.End();

            // UI layer — screen space, unaffected by the world camera's zoom/scroll.
            spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _dialogueBox.Draw(spriteBatch);
            _elevator.Draw(spriteBatch);
            _menu.Draw(spriteBatch, Game.PlayerData);

            // black going up for a map change, or lifting off after one (and
            // after a battle). whichever is stronger wins
            float black = Math.Max(
                MathHelper.Clamp(_fadeOut, 0f, 1f),
                MathHelper.Clamp(_fadeInLeft / FadeInSeconds, 0f, 1f));

            if (black > 0f)
                spriteBatch.Draw(Game.PixelTexture,
                    new Rectangle(0, 0, GameSettings.WindowWidth, GameSettings.WindowHeight),
                    Color.Black * black);

            spriteBatch.End();
        }

        // acts on whatever the player is facing. What happens comes from the
        // object's "Action" property in Tiled, so two interactables on the same
        // map can behave differently — a sign talks, the lift panel opens the
        // floor list, a door changes map.
        private void TryInteract()
        {
            if (_map == null) return;

            Point facingTile = _player.Facing.GetNeighbour(_player.TilePosition);
            Interactable interactable = _map.GetInteractableAt(facingTile.X, facingTile.Y);

            if (interactable == null) return;

            switch (interactable.Action)
            {
                // the panel in the lift. prompt first, the floor grid comes up
                // once it's dismissed
                case "elevator":
                    _dialogueBox.Open("* Please select a location.");
                    _awaitingFloorMenu = true;
                    return;

                case "shop":
                    Game.OpenVendingMachine();
                    return;

                case "warp":
                    if (interactable.TargetMap != null)
                        BeginMapChange(interactable.TargetMap, interactable.SpawnX, interactable.SpawnY);
                    return;

                // no Action, or one that isn't handled — it's a sign. Staying
                // quiet beats an empty box for an object that hasn't been given
                // its text yet
                default:
                    if (interactable.Text.Length > 0)
                        _dialogueBox.Open(interactable.Text);
                    return;
            }
        }

        // ── NPCs ─────────────────────────────────────────────────────────────

        // keeps the map's dynamic collision in sync with which NPCs are still
        // unresolved. called on every map load and every time this state
        // resumes, since a battle fought against one of them can flip its
        // RouteTracker entry while this instance was paused underneath it
        private void RefreshNpcs()
        {
            if (_map == null) return;

            foreach (NpcSpawn npc in _map.Npcs)
                _map.SetTileBlocked(npc.TileX, npc.TileY, !Game.Route.IsResolved(npc.Id));
        }

        private void DrawNpcs(SpriteBatch spriteBatch)
        {
            if (_map == null) return;

            foreach (NpcSpawn npc in _map.Npcs)
            {
                if (Game.Route.IsResolved(npc.Id)) continue;

                Spritesheet sheet = SpriteManager.GetSprite(npc.SpriteName);
                if (sheet?.Texture == null) continue;

                Rectangle src = sheet[0, 0];

                // scaled to standing height first, then bottom-anchored to its
                // tile off the SCALED size — same idea as the player's own draw
                // offset, so a tall sprite stands on the tile instead of
                // floating in it or sinking through the floor
                int tileSize = _map.TileWidth;
                int w = (int)(src.Width  * npc.Scale);
                int h = (int)(src.Height * npc.Scale);

                var dst = new Rectangle(
                    (int)(npc.TileX * tileSize + tileSize / 2f - w / 2f),
                    npc.TileY * tileSize + tileSize - h,
                    w, h);

                spriteBatch.Draw(sheet.Texture, dst, src, Color.White);
            }
        }

        // solid NPCs block movement (see TileMap.IsWalkable), so "colliding"
        // with one means the player is facing it and pushing into it. Returns
        // true if a battle was started, so Update can bail out immediately —
        // this state is about to be paused underneath BattleTransition
        private bool CheckNpcBump()
        {
            if (_map == null || _transitioning || !_player.IsMoving) return false;

            Point facingTile = _player.Facing.GetNeighbour(_player.TilePosition);

            foreach (NpcSpawn npc in _map.Npcs)
            {
                if (Game.Route.IsResolved(npc.Id)) continue;
                if (npc.TileX != facingTile.X || npc.TileY != facingTile.Y) continue;

                StartNpcBattle(npc);
                return true;
            }

            return false;
        }

        private void StartNpcBattle(NpcSpawn npc)
        {
            string teachersDir = Path.GetFullPath(
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Content", "Teachers"));
            string path = Path.Combine(teachersDir, npc.Id + ".json");
            if (!File.Exists(path))
            {
                LogDebug($"NPC BATTLE ERROR: no teacher file for \"{npc.Id}\" at {path}");
                return;
            }

            TeacherStats stats  = TeacherLoader.Load(path);
            var          teacher = new Teacher(stats);

            float tileSize = _map.TileWidth;
            Vector2 soulStart = Vector2.Transform(
                _player.WorldPosition + new Vector2(tileSize / 2f, tileSize / 2f),
                _camera.GetTransform());
            StateManager.Push(new BattleTransition(Game, StateManager, teacher, _player, soulStart, GameSettings.Zoom));
        }

#if DEBUG
        // builds a test teacher and starts a battle. Push (not Replace) so
        // this state resumes when the battle pops itself
        private void StartDebugBattle()
        {
            string teachersDir = Path.GetFullPath(
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Content", "Teachers"));
            // swap this filename to whoever you're testing (dorbendor.json, yakir.json, david.json, substitute.json)
            TeacherStats stats = TeacherLoader.Load(Path.Combine(teachersDir, "david.json"));
            var teacher = new Teacher(stats);

            // the Undertale-style intro plays first, then hands off to the battle.
            // Soul starts where the player is standing on screen (world → screen).
            float tileSize = _map != null ? _map.TileWidth : GameSettings.TileSize;
            Vector2 soulStart = Vector2.Transform(
                _player.WorldPosition + new Vector2(tileSize / 2f, tileSize / 2f),
                _camera.GetTransform());
            StateManager.Push(new BattleTransition(Game, StateManager, teacher, _player, soulStart, GameSettings.Zoom));
        }
#endif

        // ── Transitions ──────────────────────────────────────────────────────

        private void CheckTransitions()
        {
            if (_transitioning) return; // free movement, so no IsMoving gate

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
                    BeginMapChange(t.TargetMap, t.SpawnX, t.SpawnY);
                    return;
                }
            }
        }

        // Fires an on-step warp when the player is standing on a trigger tile.
        // Unlike edge transitions these are interior tiles (e.g. stepping into
        // the elevator), so there's no facing/edge check — just position.
        private void CheckWarps()
        {
            if (_transitioning) return; // free movement, so no IsMoving gate

            foreach (MapWarp w in _warps)
            {
                if (w.ContainsTile(_player.TilePosition.X, _player.TilePosition.Y))
                {
                    BeginMapChange(w.TargetMap, w.SpawnX, w.SpawnY);
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
            _player.SetTileSize(_map.TileWidth);
            _player.Teleport(spawnX, spawnY);
            _transitions = LoadTransitions(mapsDir, mapName);
            _warps       = LoadWarps(mapsDir, mapName);
            RefreshNpcs();

            EventBus.Instance.Publish(new AreaChangedEvent(_currentAreaName));
            UpdateAreaMusic();
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

        private static List<MapWarp> LoadWarps(string mapsDir, string mapName)
        {
            string path = Path.Combine(mapsDir, mapName + ".warps.json");
            if (!File.Exists(path)) return new List<MapWarp>();

            try
            {
                string json = File.ReadAllText(path);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return JsonSerializer.Deserialize<List<MapWarp>>(json, options)
                    ?? new List<MapWarp>();
            }
            catch { return new List<MapWarp>(); }
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
