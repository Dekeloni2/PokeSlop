// Game1.cs
using System;
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
using FinalProject.States;

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
            // battle themes, teachers name theirs in their JSON
            SoundManager.AddSong("yakirTheme", "Audio/Music/yakir_theme");
            SoundManager.AddSong("dbdTheme",   "Audio/Music/dbd_theme");
            SoundManager.AddSong("davidTheme", "Audio/Music/david_theme");
            SoundManager.AddSong("game_over",  "Audio/Music/game_over");
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
            // SoundManager.AddSong("battle", "Audio/battle_theme");

#if DEBUG
            LoadStartingInventory();
#endif

            StateManager.Replace(new MainMenuState(this, StateManager));
        }

        protected override void Update(GameTime gameTime)
        {
            // input first so every state sees the same snapshot this frame
            Input.Update();

            if (Input.IsKeyPressed(Keys.Escape))
                Exit();

            StateManager.Update(gameTime);
            base.Update(gameTime);
        }

#if DEBUG
        // debug builds only - gives the player one of every item so ITEM can
        // be tested until there's a real way to get items (shop/pickups)
        private void LoadStartingInventory()
        {
            string itemsPath = Path.GetFullPath(
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Content", "Items", "items.json"));
            PlayerData.Inventory.AddRange(ItemLoader.LoadAll(itemsPath));
        }
#endif

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);
            StateManager.Draw(_spriteBatch);
            base.Draw(gameTime);
        }
    }
}
