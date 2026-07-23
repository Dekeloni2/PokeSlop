using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using FinalProject.Core;
using FinalProject.Core.Audio;
using FinalProject.Core.StateMachine;
using FinalProject.Core.Text;
using FinalProject.Data;

namespace FinalProject.States
{
    // The Undertale-style GAME OVER screen. Fades in on black with a huge GAME
    // OVER, then types out one of the scenarios from gameover.json (chosen at
    // random, '|' splitting it into pages) using the snd_txtasg blip. Once the
    // last page is dismissed it revives the player and drops back to the
    // overworld, which is still paused underneath.
    public class GameOverState : GameState
    {
        private const float FadeSeconds    = 1.4f;
        private const float FadeOutSeconds = 2.5f; // slow fade to black at the end
        private const float MusicVolume    = 0.6f; 
        private const float TitleScale  = 5f;
        private const float TextScale   = 2f;
        private const float TitleY      = 70f;
        private const float TextY       = 280f;

        private readonly string        _title;
        private readonly List<string>  _pages;
        private readonly Typewriter    _typer = new Typewriter("snd_txtasg");

        private int   _page;
        private float _fadeT;
        private bool  _typing;
        private bool  _fadingOut;
        private float _fadeOutT;

        public GameOverState(Game1 game, GameStateManager sm) : base(game, sm)
        {
            string dir = Path.GetFullPath(
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Content"));
            GameOverConfig cfg = GameOverConfig.Load(Path.Combine(dir, "gameover.json"));

            _title   = cfg.Title;
            string scenario = cfg.Scenarios[Random.Shared.Next(cfg.Scenarios.Count)];
            _pages   = SplitPages(scenario);
        }

        public override void OnEnter()
        {
            // the game-over theme, turned down, looping under the text
            SoundManager.PlayMusic("game_over", true, MusicVolume);
        }

        public override void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // once the text is dismissed, fade the music and the screen to black
            // together, then revive and drop back to the overworld
            if (_fadingOut)
            {
                _fadeOutT = MathHelper.Clamp(_fadeOutT + dt / FadeOutSeconds, 0f, 1f);
                SoundManager.SetMusicVolume(MusicVolume * (1f - _fadeOutT));
                if (_fadeOutT >= 1f)
                {
                    SoundManager.StopMusic();
                    Continue();
                }
                return;
            }

            // fade the whole screen in first
            if (_fadeT < 1f)
            {
                _fadeT = MathHelper.Clamp(_fadeT + dt / FadeSeconds, 0f, 1f);
                return;
            }

            // then begin typing the first page
            if (!_typing)
            {
                _typing = true;
                _typer.SetText(_pages[_page]);
            }

            _typer.Update(dt);

            if (Game.Input.IsKeyPressed(Keys.Z) || Game.Input.IsKeyPressed(Keys.Enter))
            {
                if (!_typer.IsFullyShown) { _typer.SkipToEnd(); return; }

                // advance to the next page, or on the last one start the fade-out
                // (keeping _page valid so the final line fades out with the title)
                if (_page + 1 < _pages.Count)
                {
                    _page++;
                    _typer.SetText(_pages[_page]);
                }
                else
                {
                    _fadingOut = true;
                }
            }
        }

        private void Continue()
        {
            // revive at full HP and drop back into the overworld underneath
            Game.PlayerData.CurrentHp = Game.PlayerData.MaxHp;
            StateManager.Pop();
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            Game.GraphicsDevice.Clear(Color.Black);
            spriteBatch.Begin(samplerState: SamplerState.PointClamp);

            SpriteFont font = Game.DialogueFont;

            // fade-in ramps this up; fade-out ramps it back down to black
            float alpha = _fadeT * (1f - _fadeOutT);

            // GAME OVER, huge, centered near the top
            Vector2 titleSize = font.MeasureString(_title) * TitleScale;
            var titlePos = new Vector2((GameSettings.WindowWidth - titleSize.X) / 2f, TitleY);
            spriteBatch.DrawString(font, _title, titlePos, Color.White * alpha,
                0f, Vector2.Zero, TitleScale, SpriteEffects.None, 0f);

            // the scenario text below, typed out. centered by the FULL page width
            // so the line doesn't drift as it reveals
            if (_typing)
            {
                Vector2 fullSize = font.MeasureString(_pages[_page]) * TextScale;
                var pos = new Vector2((GameSettings.WindowWidth - fullSize.X) / 2f, TextY);
                spriteBatch.DrawString(font, _typer.VisibleText, pos, Color.White * alpha,
                    0f, Vector2.Zero, TextScale, SpriteEffects.None, 0f);
            }

            spriteBatch.End();
        }

        // split on '|' into pages, trimming and dropping blanks — same convention
        // as the teacher dialogue
        private static List<string> SplitPages(string text)
        {
            var pages = new List<string>();
            foreach (string part in (text ?? "").Split('|'))
            {
                string t = part.Trim();
                if (t.Length > 0) pages.Add(t);
            }
            if (pages.Count == 0) pages.Add("...");
            return pages;
        }
    }
}
