    // Game1.cs
using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using FinalProject.Core;
using FinalProject.Core.Audio;
using FinalProject.Core.Graphics;
using FinalProject.Core.Input;
using FinalProject.Core.StateMachine;
using FinalProject.Data;
using FinalProject.Events;
using FinalProject.States;
using FinalProject.UI;

namespace FinalProject
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        // shared systems, public so states can reach them through Game.X
        public GameStateManager StateManager { get; private set; }
        public InputManager     Input        { get; private set; }

        // 1x1 white pixel for drawing plain rectangles (HP bars, borders...)
        public Texture2D PixelTexture { get; private set; }

        public SpriteFont DialogueFont { get; private set; }

        public PlayerData PlayerData { get; private set; } = new();

        // listens for teacher fights finishing, so the ending can look at the
        // whole run. subscribes on construction, so build it before any battle
        public RouteTracker Route { get; private set; } = new();

        // the item catalog, straight from items.json
        public List<ItemData> Items { get; private set; } = new();

        // by name, as teacher JSON refers to them. null if nothing matches
        public ItemData FindItem(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;

            foreach (ItemData item in Items)
                if (string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase))
                    return item;

            return null;
        }

        private VendingMachineMenu _vendingMachine;

        // The shop lives here rather than on OverworldState because Update gates
        // the whole state machine on it — that's what freezes the world while
        // you're buying. Interactables with Action="shop" come in through here.
        public void OpenVendingMachine() => _vendingMachine?.Open(Items);

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

            DialogueFont = Content.Load<SpriteFont>("Fonts/DialogueFont");

            // Pixel Operator 8 builds with LineSpacing 9, and its descenders (g,
            // p, y) reach a full 8px below the origin — 1px of leading, so two
            // wrapped lines read as one smeared block. every text widget derives
            // its spacing from the font, so fixing it here fixes the speech
            // bubble, the dialogue box, the choice box and the action menu at once
            DialogueFont.LineSpacing = 11;
            
            new SpriteManager(Content);
            SpriteManager.AddSprite("student_world", "Sprites/Player/student_world", 4, 3);
            SpriteManager.AddSprite("soul", "Sprites/Battle/Soul", 2, 1);
            SpriteManager.AddSprite("soul_lost", "Sprites/Battle/soul_lost", 7, 1, 1, 0);
            SpriteManager.AddSprite("battleButtons", "Sprites/Battle/buttons", 2, 4, 10, 8);
            SpriteManager.AddSprite("attackZone", "Sprites/Battle/attack_minigame");
            SpriteManager.AddSprite("attackBar", "Sprites/Battle/attack_target", 2, 1, 6);
            SpriteManager.AddSprite("boat", "Sprites/AttackPatterns/boat");
            SpriteManager.AddSprite("bomb", "Sprites/AttackPatterns/bomb");
            SpriteManager.AddSprite("garlicGun", "Sprites/AttackPatterns/GarlicGun");
            SpriteManager.AddSprite("vegeta", "Sprites/AttackPatterns/Vegeta",5 , 1);
            SpriteManager.AddSprite("cloverbyte", "Sprites/AttackPatterns/cloverbyte");
            SpriteManager.AddSprite("chess", "Sprites/AttackPatterns/chess", 4, 1);
            // 32x32 frames with a 3px gutter, without the spacing the frames
            // slice offset and the flash looks like it's sliding sideways
            SpriteManager.AddSprite("warning", "Sprites/Battle/warning", 3, 1, 3);
            SpriteManager.AddSprite("napoleon", "Sprites/AttackPatterns/napoleon");
            SpriteManager.AddSprite("smoke", "Sprites/AttackPatterns/smoke");
            SpriteManager.AddSprite("yakir", "Sprites/BattleTeachers/yakir");
            SpriteManager.AddSprite("dorbendor", "Sprites/BattleTeachers/dorbendor");
            SpriteManager.AddSprite("david", "Sprites/BattleTeachers/david");
            // two different drawings of the training dummy — the small one it
            // stands as on the map, and the big one it fights as. separate keys
            // because both files are called dummy.png
            SpriteManager.AddSprite("dummy", "Sprites/World/dummy");
            SpriteManager.AddSprite("dummy_battle", "Sprites/BattleTeachers/dummy");
            SpriteManager.AddSprite("attackSlash", "Sprites/Battle/attack", 6, 1);
            // digits aren't a even grid ("1" is narrower), DamageDisplay has the rects
            SpriteManager.AddSprite("damageNumbers", "Sprites/Battle/damage");
            SpriteManager.AddSprite("textBubble", "Sprites/Battle/text_bubble");

            new SoundManager(Content);
            // text blip, the typewriters play this per character
            SoundManager.AddSound(SoundManager.TextBeepName, "Audio/Music/beep_sound");
            // elevator
            SoundManager.AddSound("doorShut", "Audio/SFX/snd_elecdoor_shut");
            SoundManager.AddSound("bell",     "Audio/SFX/snd_bell");
            SoundManager.AddSong("elevator",  "Audio/Music/mus_elevator");
            // walking-around music. OverworldState.AreaMusic says which maps get it
            SoundManager.AddSong("overworld", "Audio/Music/OverworldMusic");
            // battle themes, teachers name theirs in their JSON
            SoundManager.AddSong("yakirTheme", "Audio/Music/yakir_theme");
            SoundManager.AddSong("dbdTheme",   "Audio/Music/dbd_theme");
            SoundManager.AddSong("davidTheme", "Audio/Music/david_theme");
            SoundManager.AddSong("game_over",  "Audio/Music/game_over");
            // the ending theme. keyed by its filename, which is what endings.json
            // names in its music cues
            SoundManager.AddSong("tiltantale_ending", "Audio/Music/tiltantale_ending");
            // menus, shared by anything that has a cursor
            SoundManager.AddSound(SoundManager.MenuMove,   "Audio/SFX/snd_squeak");
            SoundManager.AddSound(SoundManager.MenuSelect, "Audio/SFX/snd_select");
            // attacking the teacher
            SoundManager.AddSound("vaporized", "Audio/SFX/snd_vaporized");
            SoundManager.AddSound("slash",  "Audio/SFX/snd_slash");
            SoundManager.AddSound("damage", "Audio/SFX/snd_damage");
            SoundManager.AddSound("snd_break1", "Audio/SFX/snd_break1");
            SoundManager.AddSound("snd_break2", "Audio/SFX/snd_break2");
            SoundManager.AddSound("snd_txtasg", "Audio/SFX/snd_txtasg");
            SoundManager.AddSound("snd_txtdavid", "Audio/SFX/snd_txtdavid");
            // napoleon sfx
            SoundManager.AddSound("rumble", "Audio/SFX/rumble");
            SoundManager.AddSound("thud",   "Audio/SFX/thud");
            // cloverbyte sfx
            SoundManager.AddSound("snd_screenshake", "Audio/SFX/snd_screenshake");
            SoundManager.AddSound("snd_heavydamage", "Audio/SFX/snd_heavydamage");
            SoundManager.AddSound("snd_spearrise",   "Audio/SFX/snd_spearrise");
            SoundManager.AddSound("snd_grab",        "Audio/SFX/snd_grab");
            // chess sfx
            SoundManager.AddSound("snd_vulkinhurt",  "Audio/SFX/snd_vulkinhurt");
            // healing, both from items and the chess board's green pieces
            SoundManager.AddSound("snd_heal_c",      "Audio/SFX/snd_heal_c");
            // the ending's phone call. AddSound swallows a missing file, so this
            // stays harmless until snd_phone.wav is dropped into Audio/SFX
            SoundManager.AddSound(SoundManager.PhoneRing, "Audio/SFX/snd_phone");
            // SoundManager.AddSong("battle", "Audio/battle_theme");

            // every item in the game, loaded once. the shop sells from it and
            // teacher drops resolve their reward name against it, so it can't
            // be debug-only the way the vending machine is
            string itemsPath = Path.GetFullPath(
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Content", "Items", "items.json"));
            Items = ItemLoader.LoadAll(itemsPath);

#if DEBUG
            LoadStartingInventory();
            _vendingMachine = new VendingMachineMenu(PixelTexture, DialogueFont);
#endif

            StateManager.Replace(new MainMenuState(this, StateManager));
        }

        protected override void Update(GameTime gameTime)
        {
            // input first so every state sees the same snapshot this frame
            Input.Update();

            if (Input.IsKeyPressed(Keys.Escape))
                Exit();

#if DEBUG
            // Press 'V' to open the vending machine anywhere in debug mode
            if (Input.IsKeyPressed(Keys.V) && _vendingMachine != null)
            {
                if (_vendingMachine.IsActive)
                {
                    _vendingMachine.Close();
                }
                else if (StateManager.CurrentState is OverworldState)
                {
                    _vendingMachine.Open(Items);
                }
            }
            
            // --- Ending Debug Keys (1 through 8) ---
            if (Input.IsKeyPressed(Keys.D1)) // 1. Pacifist
            {
                Route.Reset();
                StateManager.Replace(new EndingState(this, StateManager));
            }
            
            if (Input.IsKeyPressed(Keys.D2)) // 2. Genocide
            {
                Route.Reset();
                EventBus.Instance.Publish(new TeacherResolvedEvent("yakir", BattleOutcome.Killed));
                EventBus.Instance.Publish(new TeacherResolvedEvent("dorbendor", BattleOutcome.Killed));
                EventBus.Instance.Publish(new TeacherResolvedEvent("david", BattleOutcome.Killed));
                StateManager.Replace(new EndingState(this, StateManager));
            }
            
            if (Input.IsKeyPressed(Keys.D3)) // 3. Only Yakir Killed
            {
                Route.Reset();
                EventBus.Instance.Publish(new TeacherResolvedEvent("yakir", BattleOutcome.Killed));
                EventBus.Instance.Publish(new TeacherResolvedEvent("dorbendor", BattleOutcome.Spared));
                EventBus.Instance.Publish(new TeacherResolvedEvent("david", BattleOutcome.Spared));
                StateManager.Replace(new EndingState(this, StateManager));
            }
            
            if (Input.IsKeyPressed(Keys.D4)) // 4. Only Dorbendor Killed
            {
                Route.Reset();
                EventBus.Instance.Publish(new TeacherResolvedEvent("yakir", BattleOutcome.Spared));
                EventBus.Instance.Publish(new TeacherResolvedEvent("dorbendor", BattleOutcome.Killed));
                EventBus.Instance.Publish(new TeacherResolvedEvent("david", BattleOutcome.Spared));
                StateManager.Replace(new EndingState(this, StateManager));
            }
            
            if (Input.IsKeyPressed(Keys.D5)) // 5. Only David Killed
            {
                Route.Reset();
                EventBus.Instance.Publish(new TeacherResolvedEvent("yakir", BattleOutcome.Spared));
                EventBus.Instance.Publish(new TeacherResolvedEvent("dorbendor", BattleOutcome.Spared));
                EventBus.Instance.Publish(new TeacherResolvedEvent("david", BattleOutcome.Killed));
                StateManager.Replace(new EndingState(this, StateManager));
            }
            
            if (Input.IsKeyPressed(Keys.D6)) // 6. Yakir + Dorbendor Killed (Only David Spared)
            {
                Route.Reset();
                EventBus.Instance.Publish(new TeacherResolvedEvent("yakir", BattleOutcome.Killed));
                EventBus.Instance.Publish(new TeacherResolvedEvent("dorbendor", BattleOutcome.Killed));
                EventBus.Instance.Publish(new TeacherResolvedEvent("david", BattleOutcome.Spared));
                StateManager.Replace(new EndingState(this, StateManager));
            }
            
            if (Input.IsKeyPressed(Keys.D7)) // 7. Yakir + David Killed (Only Dorbendor Spared)
            {
                Route.Reset();
                EventBus.Instance.Publish(new TeacherResolvedEvent("yakir", BattleOutcome.Killed));
                EventBus.Instance.Publish(new TeacherResolvedEvent("dorbendor", BattleOutcome.Spared));
                EventBus.Instance.Publish(new TeacherResolvedEvent("david", BattleOutcome.Killed));
                StateManager.Replace(new EndingState(this, StateManager));
            }
            
            if (Input.IsKeyPressed(Keys.D8)) // 8. Dorbendor + David Killed (Only Yakir Spared)
            {
                Route.Reset();
                EventBus.Instance.Publish(new TeacherResolvedEvent("yakir", BattleOutcome.Spared));
                EventBus.Instance.Publish(new TeacherResolvedEvent("dorbendor", BattleOutcome.Killed));
                EventBus.Instance.Publish(new TeacherResolvedEvent("david", BattleOutcome.Killed));
                StateManager.Replace(new EndingState(this, StateManager));
            }
#endif
            
            if (_vendingMachine != null && _vendingMachine.IsActive)
            {
                _vendingMachine.Update(gameTime, Input, PlayerData);
            }
            else
            {
                StateManager.Update(gameTime);
            }
            
            base.Update(gameTime);
        }
        
        // debug builds only - gives the player one of every item so ITEM can
        // be tested until there's a real way to get items (shop/pickups)
        private void LoadStartingInventory()
        {
            PlayerData.Inventory.AddRange(Items);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);
            StateManager.Draw(_spriteBatch);
            
            if (_vendingMachine != null && _vendingMachine.IsActive)
            {
                // PointClamp, or the pixel font gets bilinear-filtered into mush
                _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
                _vendingMachine.Draw(_spriteBatch, PlayerData);
                _spriteBatch.End();
            }
            
            base.Draw(gameTime);
        }
    }
}
