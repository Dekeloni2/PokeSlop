// States/OverworldState.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using FinalProject.Battle;
using FinalProject.Battle.Teachers;
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
        private List<BossGate>      _bossGates    = new();
        private bool _transitioning = false; // prevents repeated trigger on failed load

        // Undertale-style textbox — owns input while open (see Update below).
        private DialogueBox _dialogueBox;
        // player menu
        private OverworldMenu _menu;

        // the one soul-cursor Yes/No box every prompt in this file shares —
        // boss gates, the toilet, the cheese, the keypad. Only one of them is
        // ever open at a time, so a single instance is all that's needed; the
        // *ChoiceActive flags by each feature below are what tell its
        // resolution (see the _choiceBox.IsActive block in Update) which
        // On...ChoiceMade to call.
        private ChoiceBox _choiceBox;

        // ── Boss gates ───────────────────────────────────────────────────────
        // walking into a BossGate's tiles asks "enter his class?" on the
        // shared choice box above, plays the teacher's own greeting on Yes,
        // then hands off to the battle transition. See CheckBossGates/OpenBossGate.
        private BossGate      _pendingGate;         // the gate currently being resolved
        private TeacherStats  _pendingGateTeacher;   // its teacher, loaded once and carried through the steps
        private bool          _awaitingBossChoice;   // the prompt just closed, bring up Yes/No
        private bool          _awaitingBossBattle;    // the teacher's greeting just closed, start the fight
        private BossGate      _bossGateCooldown;     // last gate walked into — held until the player steps off so a "No" (or standing still) doesn't re-ask every frame

        // the genocide prompt already was him talking, face and voice — set
        // whenever that's what was shown, so OnBossChoiceMade knows a second
        // greeting right after "Yes" would just repeat the same beat
        private bool _pendingGateGenocidePrompt;

        // ── Toilets ──────────────────────────────────────────────────────────
        // an Interactable with Action "toilet" (the stalls in the toilet map)
        // asks "flush it?" on the same shared Yes/No ChoiceBox the boss gates
        // use. _toiletChoiceActive is what tells the shared box's resolution
        // below which of the two flows ("enter his class?" or "flush it?") to
        // route the answer to, since only one _choiceBox exists
        private bool  _awaitingToiletChoice; // the prompt just closed, bring up Yes/No
        private bool  _toiletChoiceActive;   // the Yes/No currently up is this one, not a boss gate's

        // counts down snd_toilet's own length after "Yes" — the payoff (the
        // victory jingle and the line) only fires once it hits zero, so the
        // flush is heard in full before either one steps on it
        private float _toiletFlushTimer;

        // ── Pickable Cheese ──────────────────────────────────────────────────
        // tiltan_hall's Action "cheese" — same shared Yes/No ChoiceBox again,
        // see _toiletChoiceActive above for why a dispatch flag is needed
        private bool  _awaitingCheeseChoice; // the joke + prompt just closed, bring up Yes/No
        private bool  _cheeseChoiceActive;   // the Yes/No currently up is this one

        // which Interactable asked, so a "Yes" knows which map tiles to swap
        // to the empty plate — held across the Yes/No wait, not read until then
        private Interactable _pendingCheeseSpot;

        // the sprite is built from two tiles side by side (cheese.tsx); these
        // are what they turn into once it's picked up — the empty table sits
        // right below the full one in the same tileset (1383/1384 -> 1385/1386)
        private const int CheeseFullGidA  = 1383;
        private const int CheeseFullGidB  = 1384;
        private const int CheeseEmptyGidA = 1385;
        private const int CheeseEmptyGidB = 1386;

        // ── Keypad ───────────────────────────────────────────────────────────
        // floor_2's Action "keypad" — same shared Yes/No ChoiceBox once more,
        // see _toiletChoiceActive above for why a dispatch flag is needed.
        // Whatever code gets dialed in, there's no correct one — Cloverbyte
        // never gave the player the real digits, so confirming always answers
        // "Incorrect code."
        private bool _awaitingKeypadChoice; // the prompt just closed, bring up Yes/No
        private bool _keypadChoiceActive;   // the Yes/No currently up is this one
        private KeypadEntry _keypadEntry;

        // Cache of already-loaded maps so backtracking doesn't re-parse JSON from disk
        private readonly Dictionary<string, TileMap> _mapCache = new();

        // ── Lifecycle ────────────────────────────────────────────────────────

        public override void OnEnter()
        {
            _player      = new Player(Game, 6, 14);
            _camera      = new Camera();
            _elevator    = new ElevatorSequence(Game.PixelTexture, Game.DialogueFont);
            _choiceBox   = new ChoiceBox(Game.PixelTexture, Game.DialogueFont);
            _keypadEntry = new KeypadEntry(Game.PixelTexture, Game.DialogueFont);
            // TODO: swap back to the real starting map once the tileset rework
            // lands — pointed at "entrance" for now to test the Interactables layer.
            LoadMap("entrance", 6, 14);

            _dialogueBox = new DialogueBox(Game.PixelTexture, Game.DialogueFont);
            _menu        = new OverworldMenu(Game, Game.PixelTexture, Game.DialogueFont);
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

            // same track as the hall — it's just a room off of it, walking in
            // shouldn't cut the music. mapping both to "overworld" means
            // UpdateAreaMusic sees no change and never restarts the track
            ["toilet"] = "overworld",
        };

        // per-track volume, on top of the player's own Music Volume setting.
        // The hallway loops constantly in the background behind whatever else
        // is going on, so it runs quieter than something you'd actually stop
        // to listen to. 1f (full) for anything not listed here.
        private static readonly Dictionary<string, float> AreaMusicVolume = new()
        {
            ["overworld"]          = 0.55f,
            ["overworld_genocide"] = 0.55f,
        };

        // Undertale's genocide route drops the music's pitch once it's past
        // the point of no return. This is a much narrower version of that —
        // just the hallway's own track — and the checkpoint is killing both
        // of these (Dorbendor's gate can't even be reached before then, per
        // his Requires). RouteTracker already knows how each fight ended, so
        // this just asks it. Battle themes aren't touched, only "overworld".
        private static readonly string[] GenocideCheckpoint = { "david", "yakir" };
        private const string GenocideOverworldTrack = "overworld_genocide";

        private bool GenocideCheckpointCleared()
        {
            foreach (string teacherId in GenocideCheckpoint)
                if (Game.Route.OutcomeFor(teacherId) != BattleOutcome.Killed)
                    return false;
            return true;
        }

        // whatever this area asked for last, so walking between two maps that
        // share a track doesn't restart it from the top
        private string _areaSong;

        private void UpdateAreaMusic(bool force = false)
        {
            AreaMusic.TryGetValue(_currentAreaName ?? "", out string song);

            // same map, swapped for its pitched-down twin once the checkpoint
            // clears — everything else about "which track plays here" is
            // still just what AreaMusic says
            if (song == "overworld" && GenocideCheckpointCleared())
                song = GenocideOverworldTrack;

            if (!force && song == _areaSong) return;

            _areaSong = song;

            if (song == null)
            {
                SoundManager.StopMusic();
            }
            else
            {
                float volume = AreaMusicVolume.TryGetValue(song, out float v) ? v : 1f;
                SoundManager.PlayMusic(song, volume: volume);
            }
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

                // the panel and the keypad both sit above the spawn point on
                // their respective maps, so arriving there always faces up
                // rather than keeping whatever direction the player walked in
                // facing on the map they just left
                if (map == "elevator" || map == "floor_2")
                    _player.Face(Direction.Up);

                // the very first time the elevator (or however else) lands
                // the player on tiltan_hall, the Undertale-style credits play
                // instead of fading straight in — Resume() (called when it
                // pops) picks up the fade-in from there, so the sequence
                // reads as one continuous transition rather than a detour
                if (map == "tiltan_hall" && !Game.HasFirstCreditPlayed)
                {
                    Game.HasFirstCreditPlayed = true;
                    StateManager.Push(new CreditsIntroState(Game, StateManager));
                    return;
                }

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

            // the shared Yes/No box owns input while it's up — a boss gate's
            // "enter his class?", the toilet's "flush it?", the cheese's
            // "pick it up?", or the keypad's "type code in?" — see the
            // matching *ChoiceActive flags for which one is currently open
            if (_choiceBox.IsActive)
            {
                if (_choiceBox.Update(Game.Input))
                {
                    if (_toiletChoiceActive)
                    {
                        _toiletChoiceActive = false;
                        OnToiletChoiceMade(_choiceBox.SelectedIndex);
                    }
                    else if (_cheeseChoiceActive)
                    {
                        _cheeseChoiceActive = false;
                        OnCheeseChoiceMade(_choiceBox.SelectedIndex);
                    }
                    else if (_keypadChoiceActive)
                    {
                        _keypadChoiceActive = false;
                        OnKeypadChoiceMade(_choiceBox.SelectedIndex);
                    }
                    else
                    {
                        OnBossChoiceMade(_choiceBox.SelectedIndex);
                    }
                }
                return;
            }

            // holding still through the flush itself, same idea as the fade
            // wait above — the jingle and the line only land once it's over
            if (_toiletFlushTimer > 0f)
            {
                _toiletFlushTimer -= dt;
                if (_toiletFlushTimer <= 0f)
                {
                    SoundManager.Play("snd_dumbvictory");
                    _dialogueBox.Open("You have flushed the toilet. You successfully wasted water.");
                }
                return;
            }

            // the keypad owns input while it's up — confirming a code always
            // fails, see the field comment on _keypadEntry
            if (_keypadEntry.IsActive)
            {
                if (_keypadEntry.Update(Game.Input))
                    _dialogueBox.Open("Incorrect code.");
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

            // Open Menu when C (or LeftControl) is pressed ─
            if (Game.Input.IsKeyPressed(Keys.C) || Game.Input.IsKeyPressed(Keys.LeftControl))
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

            // the "enter his class?" prompt just closed, bring up the Yes/No choice
            if (_awaitingBossChoice)
            {
                _awaitingBossChoice = false;
                _choiceBox.Open(new List<string> { "Yes", "No" });
                return;
            }

            // the "flush it?" prompt just closed, bring up its own Yes/No
            if (_awaitingToiletChoice)
            {
                _awaitingToiletChoice = false;
                _toiletChoiceActive   = true;
                _choiceBox.Open(new List<string> { "Yes", "No" });
                return;
            }

            // the joke + "pick up cheese?" prompt just closed, bring up its own Yes/No
            if (_awaitingCheeseChoice)
            {
                _awaitingCheeseChoice = false;
                _cheeseChoiceActive   = true;
                _choiceBox.Open(new List<string> { "Yes", "No" });
                return;
            }

            // the "type code in?" prompt just closed, bring up its own Yes/No
            if (_awaitingKeypadChoice)
            {
                _awaitingKeypadChoice = false;
                _keypadChoiceActive   = true;
                _choiceBox.Open(new List<string> { "Yes", "No" });
                return;
            }

            // the teacher's own greeting just closed, off to the fight
            if (_awaitingBossBattle)
            {
                _awaitingBossBattle = false;
                StartBossBattle();
                return;
            }

            if (Game.Input.IsKeyPressed(Keys.Z) || Game.Input.IsKeyPressed(Keys.Enter))
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
            CheckBossGates();
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
            _choiceBox.Draw(spriteBatch);
            _keypadEntry.Draw(spriteBatch);
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

                // a toilet stall — see OnToiletChoiceMade for the payoff
                case "toilet":
                    _dialogueBox.Open("A toilet, flush it?");
                    _awaitingToiletChoice = true;
                    return;

                // the hall's pickable cheese — already gone once picked up,
                // so it quietly does nothing rather than re-running the joke
                case "cheese":
                    if (Game.HasPickedUpCheese) return;

                    _pendingCheeseSpot = interactable;
                    _dialogueBox.Open("This is here because we don't have any assets.|"
                        + "Wait a minute, this cheese is pickable?|Pick up cheese?");
                    _awaitingCheeseChoice = true;
                    return;

                // floor_2's keypad — see OnKeypadChoiceMade for the payoff
                case "keypad":
                    _dialogueBox.Open("There is a keypad that allows you access to Cloverbyte offices. Type code in?");
                    _awaitingKeypadChoice = true;
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

            foreach (Npc npc in _map.Npcs)
                _map.SetTileBlocked(npc.TileX, npc.TileY, !Game.Route.IsResolved(npc.Id));
        }

        private void DrawNpcs(SpriteBatch spriteBatch)
        {
            if (_map == null) return;

            foreach (Npc npc in _map.Npcs)
            {
                if (Game.Route.IsResolved(npc.Id)) continue;
                npc.Draw(spriteBatch, _map.TileWidth);
            }
        }

        // solid NPCs block movement (see TileMap.IsWalkable), so "colliding"
        // with one means the player is facing it and pushing into it. Returns
        // true if an interaction fired, so Update can bail out immediately —
        // e.g. a battle NPC is about to pause this state underneath
        // BattleTransition. What actually happens is entirely up to the NPC's
        // own Kind (see Npc/INpcInteraction) — this only decides WHEN.
        private bool CheckNpcBump()
        {
            if (_map == null || _transitioning || !_player.IsMoving) return false;

            Point facingTile = _player.Facing.GetNeighbour(_player.TilePosition);

            foreach (Npc npc in _map.Npcs)
            {
                if (Game.Route.IsResolved(npc.Id)) continue;
                if (npc.TileX != facingTile.X || npc.TileY != facingTile.Y) continue;

                npc.Interact(new NpcInteractionContext(StartBattleById));
                return true;
            }

            return false;
        }

        // what a battle-kind NPC interaction calls into — the only piece of
        // starting a fight that isn't generic across NPCs, boss gates and the
        // debug battle, since it needs the camera/player position PushBattle
        // reads (see PushBattle).
        private void StartBattleById(string id)
        {
            TeacherStats stats = LoadTeacherStatsById(id, "NPC BATTLE ERROR");
            if (stats == null) return;

            PushBattle(new Teacher(stats));
        }

        // Content/Teachers/{id}.json -> loaded stats, or null (logged to
        // map_debug.txt) if the file doesn't exist. Shared by every way a
        // fight can start — NPC bump, debug battle, and boss gates.
        private static TeacherStats LoadTeacherStatsById(string id, string errorTag = "TEACHER LOAD ERROR")
        {
            string path = ContentPaths.Under("Teachers", id + ".json");
            if (!File.Exists(path))
            {
                LogDebug($"{errorTag}: no teacher file for \"{id}\" at {path}");
                return null;
            }

            return TeacherLoader.Load(path);
        }

        // The Undertale-style intro, soul starting where the player stands on
        // screen (world → screen), same for every kind of encounter.
        private void PushBattle(Teacher teacher)
        {
            float tileSize = _map != null ? _map.TileWidth : GameSettings.TileSize;
            Vector2 soulStart = Vector2.Transform(
                _player.WorldPosition + new Vector2(tileSize / 2f, tileSize / 2f),
                _camera.GetTransform());
            StateManager.Push(new BattleTransition(Game, StateManager, teacher, _player, soulStart, GameSettings.Zoom));
        }

        // ── Boss gates ───────────────────────────────────────────────────────

        // A gate's tiles sit on the Objects layer and block movement like a
        // wall, so this fires the same way CheckNpcBump does — face it and
        // walk into it — rather than by standing on an open tile like
        // CheckWarps. It asks first instead of acting immediately though, so
        // it needs its own cooldown: without one, holding a direction into
        // the door would reopen the prompt every single frame.
        private void CheckBossGates()
        {
            if (_map == null || _transitioning || _pendingGate != null) return;

            Point facingTile = _player.Facing.GetNeighbour(_player.TilePosition);

            BossGate facedGate = null;
            foreach (BossGate gate in _bossGates)
            {
                if (gate.ContainsTile(facingTile.X, facingTile.Y))
                {
                    facedGate = gate;
                    break;
                }
            }

            // turned away (or stepped back) from whatever was last asked
            // about — arm it again so walking back into it re-asks
            if (facedGate != _bossGateCooldown)
                _bossGateCooldown = null;

            if (!_player.IsMoving || facedGate == null || facedGate == _bossGateCooldown) return;

            OpenBossGate(facedGate);
        }

        // Walked into a gate's tiles for the first time this approach. Four
        // outcomes: the teacher's already been resolved (RouteTracker says
        // how — SparedText or KilledText, whichever fits), their Requires
        // aren't all settled yet (show LockedText), or the door's open and
        // it's time to ask.
        private void OpenBossGate(BossGate gate)
        {
            _bossGateCooldown = gate;

            BattleOutcome? outcome = Game.Route.OutcomeFor(gate.TeacherId);
            if (outcome != null)
            {
                string text = outcome == BattleOutcome.Spared ? gate.SparedText : gate.KilledText;
                if (!string.IsNullOrWhiteSpace(text))
                    _dialogueBox.Open(text);
                return;
            }

            TeacherStats stats = LoadTeacherStatsById(gate.TeacherId, "BOSS GATE ERROR");
            if (stats == null) return;

            if (!Game.Route.HasResolvedAll(stats.Requires))
            {
                _dialogueBox.Open(stats.LockedText ?? "This class isn't open yet.");
                return;
            }

            _pendingGate        = gate;
            _pendingGateTeacher = stats;

            // he can tell what route the player is on before the fight even
            // starts — Pacifist and Neutral read the same (PromptText), a
            // narrator-style line same as every other gate's prompt. The
            // Genocide line is different in kind, not just content: it's him
            // actually talking, so unlike the plain prompt it gets his face
            // and voice, same as his OnEncounter greeting would
            bool genocidePrompt = GenocideCheckpointCleared() && !string.IsNullOrWhiteSpace(gate.GenocidePromptText);
            _pendingGateGenocidePrompt = genocidePrompt;

            if (genocidePrompt)
                _dialogueBox.Open(gate.GenocidePromptText, Speaker.ForTeacher(stats));
            else
                _dialogueBox.Open(gate.PromptText);

            _awaitingBossChoice = true;
        }

        // Yes/No answered. Yes moves on to the teacher's own greeting (or
        // straight to the fight if he has none to give); anything else drops
        // the gate — _bossGateCooldown is already set, so it won't re-ask
        // until the player steps off the tiles and back on.
        private void OnBossChoiceMade(int selectedIndex)
        {
            const int YesIndex = 0;

            if (selectedIndex != YesIndex || _pendingGateTeacher == null)
            {
                _pendingGate        = null;
                _pendingGateTeacher = null;
                return;
            }

            // the genocide prompt already had him speak, in his own face and
            // voice — skip the greeting so "Yes" doesn't immediately repeat
            // the beat with a second, unrelated line
            string greeting = _pendingGateGenocidePrompt ? null : _pendingGateTeacher.Dialogue?.OnEncounter;
            if (string.IsNullOrWhiteSpace(greeting))
            {
                StartBossBattle();
                return;
            }

            _dialogueBox.Open(greeting, Speaker.ForTeacher(_pendingGateTeacher));
            _awaitingBossBattle = true;
        }

        // "flush it?" answered. No just drops it, same as a boss gate's — no
        // cooldown needed here since the prompt only ever comes from a fresh
        // Z press, not from standing on a tile like a boss gate's does
        private void OnToiletChoiceMade(int selectedIndex)
        {
            const int YesIndex = 0;
            if (selectedIndex != YesIndex) return;

            SoundManager.Play("snd_toilet");

            // the payoff waits for the flush to actually finish — see
            // _toiletFlushTimer's tick in Update
            _toiletFlushTimer = (float)SoundManager.GetDuration("snd_toilet").TotalSeconds;
        }

        // "pick up cheese?" answered. No just leaves it sitting there — no
        // cooldown needed, same reasoning as the toilet's.
        private void OnCheeseChoiceMade(int selectedIndex)
        {
            const int YesIndex = 0;

            Interactable spot = _pendingCheeseSpot;
            _pendingCheeseSpot = null;

            if (selectedIndex != YesIndex) return;

            if (Game.PlayerData.IsInventoryFull)
            {
                _dialogueBox.Open("The cheese does not fit in your inventory because you have no space.");
                return;
            }

            ItemData cheese = Game.Items.Find(i => i.Name == "Cheese");
            if (cheese == null) return; // items.json is missing the entry — nothing to give

            Game.PlayerData.Inventory.Add(cheese);
            Game.HasPickedUpCheese = true;
            SoundManager.Play("snd_buyitem");

            if (_map != null && spot != null)
                SwapCheeseTile(spot);

            _dialogueBox.Open("You picked up Cheese. Cheese is now in your inventory.");
        }

        // swaps every full-plate cell within the interactable's own footprint
        // to the empty variant, rather than hardcoding tile coordinates here —
        // so this stays correct even if the cheese ever moves in Tiled
        private void SwapCheeseTile(Interactable spot)
        {
            for (int y = spot.TileMinY; y <= spot.TileMaxY; y++)
            for (int x = spot.TileMinX; x <= spot.TileMaxX; x++)
            {
                int gid = _map.GetObjectsTileGid(x, y);
                if (gid == CheeseFullGidA)      _map.SetObjectsTile(x, y, CheeseEmptyGidA);
                else if (gid == CheeseFullGidB) _map.SetObjectsTile(x, y, CheeseEmptyGidB);
            }
        }

        // "type code in?" answered. No leaves the keypad alone; Yes opens the
        // dialer — see the _keypadEntry.IsActive block in Update for the
        // "Incorrect code." payoff once a code is actually confirmed
        private void OnKeypadChoiceMade(int selectedIndex)
        {
            const int YesIndex = 0;
            if (selectedIndex != YesIndex) return;

            _keypadEntry.Open();
        }

        private void StartBossBattle()
        {
            TeacherStats stats  = _pendingGateTeacher;
            _pendingGate        = null;
            _pendingGateTeacher = null;

            if (stats == null) return;

            PushBattle(new Teacher(stats));
        }

#if DEBUG
        // builds a test teacher and starts a battle. Push (not Replace) so
        // this state resumes when the battle pops itself
        private void StartDebugBattle()
        {
            // swap this id to whoever you're testing (dorbendor, yakir, david, substitute)
            TeacherStats stats = LoadTeacherStatsById("dorbendor", "DEBUG BATTLE ERROR");
            if (stats == null) return;

            PushBattle(new Teacher(stats));
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

            string mapsDir = ContentPaths.Under("Maps");

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
            _bossGates   = LoadBossGates(mapsDir, mapName);
            RefreshNpcs();

            // a gate mid-conversation on the map we just left doesn't carry
            // over — new map, nothing pending, nothing on cooldown
            _pendingGate        = null;
            _pendingGateTeacher = null;
            _awaitingBossChoice = false;
            _awaitingBossBattle = false;
            _bossGateCooldown   = null;
            _choiceBox.Close();

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

        private static List<BossGate> LoadBossGates(string mapsDir, string mapName)
        {
            string path = Path.Combine(mapsDir, mapName + ".bossgates.json");
            if (!File.Exists(path)) return new List<BossGate>();

            try
            {
                string json = File.ReadAllText(path);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return JsonSerializer.Deserialize<List<BossGate>>(json, options)
                    ?? new List<BossGate>();
            }
            catch { return new List<BossGate>(); }
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
