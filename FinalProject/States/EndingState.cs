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
using FinalProject.UI;

namespace FinalProject.States;

// The end of a run, reached straight from the last fight. Fades up on black,
// the phone rings, the surviving teachers talk about the student in their own
// faces and voices, and then the title card closes it out.
//
// Which of the eight scripts plays is decided by exactly who was killed — see
// EndingConfig.For. All the wording lives in Content/endings.json.
public class EndingState : GameState
{
    private enum Phase { FadeIn, Call, Cards, FadeOut }

    private const float FadeInSeconds  = 1.6f;
    private const float FadeOutSeconds = 3.0f;
    private const float MusicVolume    = 0.6f;

    // beat of silence after the fade before the phone starts ringing
    private const float RingDelaySeconds = 0.6f;

    // slower than regular dialogue — an ending should be read, not skimmed
    private const float TextCharsPerSecond = 26f;

    private const float TitleScale = 3f;
    private const float TextScale  = 2f;

    private const float TitleY     = 64f;
    private const float TextY      = 190f;
    private const float PromptY    = 420f;
    private const int   WrapMargin = 60;

    private readonly EndingText  _ending;
    private readonly DialogueBox _dialogue;
    private readonly Typewriter  _typer;

    // teacher data is only loaded for the ids this ending actually speaks with
    private readonly Dictionary<string, Speaker> _speakers = new(StringComparer.OrdinalIgnoreCase);

    // closing narration, pre-wrapped so it never runs off the screen
    private readonly List<List<string>> _pages = new();

    private Phase _phase = Phase.FadeIn;
    private float _fadeT;
    private float _fadeOutT;
    private float _ringDelay;

    private int   _line;
    private int   _page;
    private bool  _typing;
    private float _blink;

    private bool IsLastPage => _page >= _pages.Count - 1;

    public EndingState(Game1 game, GameStateManager stateManager) : base(game, stateManager)
    {
        string path = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Content", "endings.json"));

        _ending   = EndingConfig.Load(path).For(Game.Route.KilledIds);
        _dialogue = new DialogueBox(Game.PixelTexture, Game.DialogueFont);
        _typer    = new Typewriter(null, TextCharsPerSecond);

        float wrapWidth = GameSettings.WindowWidth - WrapMargin * 2;
        foreach (string page in _ending.Pages)
            _pages.Add(TextWrap.ToLines(Game.DialogueFont, Substitute(page), wrapWidth, TextScale));
    }

    public override void OnEnter()
    {
        if (!string.IsNullOrWhiteSpace(_ending.Music))
            SoundManager.PlayMusic(_ending.Music, true, MusicVolume);
    }

    // ── Speakers ─────────────────────────────────────────────────────────────

    // A line's speaker is a teacher id, so his face and blip come from the same
    // JSON the fight used. Narration lines have no speaker and get neither.
    private Speaker SpeakerFor(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;
        if (_speakers.TryGetValue(id, out Speaker cached)) return cached;

        string path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "Content", "Teachers", id + ".json"));

        Speaker speaker = null;
        if (File.Exists(path))
        {
            try { speaker = Speaker.ForTeacher(TeacherLoader.Load(path)); }
            catch { /* an unreadable teacher just speaks facelessly */ }
        }

        _speakers[id] = speaker;
        return speaker;
    }

    // ── Update ───────────────────────────────────────────────────────────────

    public override void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _blink += dt;

        switch (_phase)
        {
            case Phase.FadeIn:  UpdateFadeIn(dt);  break;
            case Phase.Call:    UpdateCall(gameTime, dt); break;
            case Phase.Cards:   UpdateCards(dt);   break;
            case Phase.FadeOut: UpdateFadeOut(dt); break;
        }
    }

    // fade up before anything can be read — this also eats the keypress that
    // ended the fight, so the first line can't be skipped by a held Z
    private void UpdateFadeIn(float dt)
    {
        _fadeT = MathHelper.Clamp(_fadeT + dt / FadeInSeconds, 0f, 1f);
        if (_fadeT < 1f) return;

        _phase = _ending.Lines.Count > 0 ? Phase.Call : Phase.Cards;
    }

    private void UpdateCall(GameTime gameTime, float dt)
    {
        // hold a moment on empty black, then the phone starts
        if (_ringDelay < RingDelaySeconds)
        {
            _ringDelay += dt;
            if (_ringDelay < RingDelaySeconds) return;

            SoundManager.Play(SoundManager.PhoneRing);
            OpenLine();
            return;
        }

        _dialogue.Update(gameTime, Game.Input);

        // the box closes itself once its last page is dismissed
        if (_dialogue.IsActive) return;

        _line++;
        if (_line < _ending.Lines.Count) OpenLine();
        else                             _phase = Phase.Cards;
    }

    private void OpenLine()
    {
        EndingLine line = _ending.Lines[_line];
        _dialogue.Open(Substitute(line.Text), SpeakerFor(line.Speaker));
    }

    private void UpdateCards(float dt)
    {
        // an ending with no closing narration goes straight out
        if (_pages.Count == 0)
        {
            _phase = Phase.FadeOut;
            return;
        }

        if (!_typing)
        {
            _typing = true;
            _typer.SetText(CurrentPageText);
        }

        _typer.Update(dt);

        if (!Game.Input.IsKeyPressed(Keys.Z) && !Game.Input.IsKeyPressed(Keys.Enter)) return;

        // like GameOverState: a press mid-type does nothing, not even skip
        if (!_typer.IsFullyShown) return;

        if (!IsLastPage)
        {
            _page++;
            _typer.SetText(CurrentPageText);
            _blink = 0f;
        }
        else
        {
            _phase = Phase.FadeOut;
        }
    }

    private void UpdateFadeOut(float dt)
    {
        _fadeOutT = MathHelper.Clamp(_fadeOutT + dt / FadeOutSeconds, 0f, 1f);

        if (!string.IsNullOrWhiteSpace(_ending.Music))
            SoundManager.SetMusicVolume(MusicVolume * (1f - _fadeOutT));

        if (_fadeOutT < 1f) return;

        if (!string.IsNullOrWhiteSpace(_ending.Music))
            SoundManager.StopMusic();

        // a fresh run starts clean
        Game.Route.Reset();
        StateManager.Replace(new MainMenuState(Game, StateManager));
    }

    private string CurrentPageText => string.Join("\n", _pages[_page]);

    // {killed}/{spared} are the raw counts; the *Teachers forms carry the noun
    // so a line reads "1 teacher" rather than "1 teacher(s)"
    private string Substitute(string text)
    {
        int killed = Game.Route.Killed;
        int spared = Game.Route.Spared;

        return (text ?? "")
            .Replace("{killed}", killed.ToString())
            .Replace("{spared}", spared.ToString())
            .Replace("{killedTeachers}", Pluralize(killed))
            .Replace("{sparedTeachers}", Pluralize(spared));
    }

    private static string Pluralize(int count)
        => count == 1 ? "1 teacher" : $"{count} teachers";

    // ── Draw ─────────────────────────────────────────────────────────────────

    public override void Draw(SpriteBatch spriteBatch)
    {
        Game.GraphicsDevice.Clear(Color.Black);
        spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        float alpha = _fadeT * (1f - _fadeOutT);

        // The call plays on bare black — the title would give the ending away
        // before the teachers have said anything, so it waits for the cards.
        if (_phase == Phase.Call)
        {
            _dialogue.Draw(spriteBatch);
        }
        else if (_phase != Phase.FadeIn)
        {
            DrawCards(spriteBatch, alpha);
        }

        // the fade-out dims whatever was on screen, including the dialogue box
        if (_fadeOutT > 0f)
            spriteBatch.Draw(Game.PixelTexture,
                new Rectangle(0, 0, GameSettings.WindowWidth, GameSettings.WindowHeight),
                Color.Black * _fadeOutT);

        spriteBatch.End();
    }

    private void DrawCards(SpriteBatch spriteBatch, float alpha)
    {
        SpriteFont font = Game.DialogueFont;

        DrawCentered(spriteBatch, font, _ending.Title, TitleY, TitleScale,
            _ending.ResolvedTitleColor * alpha);

        if (!_typing) return;

        DrawBody(spriteBatch, font, alpha);

        if (_phase == Phase.Cards && _typer.IsFullyShown && (int)(_blink * 2f) % 2 == 0)
        {
            string prompt = IsLastPage ? "[Z] TITLE SCREEN" : ">";
            DrawCentered(spriteBatch, font, prompt, PromptY, TextScale, Color.Gray * alpha);
        }
    }

    // Each line is placed by the width of its FINISHED text, so a line doesn't
    // creep sideways as the typewriter fills it in.
    private void DrawBody(SpriteBatch spriteBatch, SpriteFont font, float alpha)
    {
        List<string> full = _pages[_page];
        string[] visible  = _typer.VisibleText.Split('\n');
        float lineHeight  = font.LineSpacing * TextScale;

        for (int i = 0; i < visible.Length && i < full.Count; i++)
        {
            float width = font.MeasureString(full[i]).X * TextScale;
            var pos = new Vector2((GameSettings.WindowWidth - width) / 2f, TextY + i * lineHeight);

            spriteBatch.DrawString(font, visible[i], pos, Color.White * alpha,
                0f, Vector2.Zero, TextScale, SpriteEffects.None, 0f);
        }
    }

    private static void DrawCentered(SpriteBatch spriteBatch, SpriteFont font, string text,
                                     float y, float scale, Color color)
    {
        float width = font.MeasureString(text).X * scale;
        var pos = new Vector2((GameSettings.WindowWidth - width) / 2f, y);

        spriteBatch.DrawString(font, text, pos, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
    }
}
