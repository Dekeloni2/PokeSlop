using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FinalProject.Core;
using FinalProject.Core.Audio;

namespace FinalProject.Battle.Patterns;

// Yakir's lesson. instead of dodging, the player answers data type questions by
// walking the soul onto one of four buttons in the corners of the arena. no
// confirm key, touching a button is the answer.
// right answer rings the bell and moves on, wrong one hurts and the question
// stays up. the soul is put back in the middle after either
public class DataTypePattern : IBulletPattern
{
    // the turn ends when the quiz does, not on a timer. once the last answer is
    // in, Duration drops to just past that moment and DodgePhase wraps up
    public float Duration => _doneTime >= 0f ? _doneTime + EndPause : 60f;

    private const float EndPause = 1.2f; // beat after the last answer

    private const float IntroSeconds  = 2.5f; // his lead in before the first one
    private const float AnswerCooldown = 0.5f; // stops one touch answering twice
    private const int   WrongDamage   = 3;

    // sits low on the screen on purpose, Yakir stands above it
    private const int BoxWidth  = 400;
    private const int BoxHeight = 200;
    private const int BoxTop    = 252;
    private const float ResizeSeconds = 0.6f;

    private const int ButtonWidth  = 104;
    private const int ButtonHeight = 40;
    private const int ButtonPad    = 16;
    private const int BorderThickness = 2;
    private const float LabelScale = 2f;

    private static readonly Color ButtonColor = new Color(255, 140, 30);

    // the four buttons, in corner order: top left, top right, bottom left, bottom right
    private static readonly string[] Labels = { "INT", "STRING", "FLOAT", "BOOL" };

    // each question and which button index answers it
    private static readonly (string Line, int Answer)[] Questions =
    {
        ("students = 10",        0), // INT
        ("gravity = 0.55f",      2), // FLOAT
        ("name = \"Yakir\"",     1), // STRING
        ("isPressed = false",    3), // BOOL
    };

    private float _elapsed;
    private float _cooldown;
    private int   _question;
    private float _doneTime = -1f;

    public bool IsIntro  => _elapsed < IntroSeconds;
    public bool IsDone   => _question >= Questions.Length;

    // what DodgePhase shows above the arena
    public string PromptText => IsDone
        ? "* Lesson complete."
        : IsIntro
            ? "* Our first lesson is about data types!"
            : "* What data type is  " + Questions[_question].Line + "  ?";

    public void Start(DodgeContext context)
    {
        context.ResizeBoxTo(
            new Rectangle(GameSettings.WindowWidth / 2 - BoxWidth / 2, BoxTop,
                          BoxWidth, BoxHeight),
            ResizeSeconds);

        // he teaches the lesson in person for this one
        context.SetTeacherVisible(true);
        context.SetTeacherSpeech(PromptText);
    }

    public void Update(GameTime gameTime, DodgeContext context)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _elapsed += dt;

        if (_cooldown > 0f) _cooldown -= dt;

        // keeps his bubble in step with the intro ending and each answer
        context.SetTeacherSpeech(PromptText);

        if (IsIntro || IsDone || _cooldown > 0f) return;

        // walking onto a button is the answer, there's no confirm key
        for (int i = 0; i < Labels.Length; i++)
        {
            if (!ButtonRect(context.CurrentBox, i).Contains(context.HitboxPosition)) continue;

            Answer(context, i);
            return;
        }
    }

    private void Answer(DodgeContext context, int chosen)
    {
        _cooldown = AnswerCooldown;

        if (chosen == Questions[_question].Answer)
        {
            SoundManager.Play("bell");
            _question++;

            if (IsDone) _doneTime = _elapsed;
        }
        else
        {
            context.DamagePlayer(WrongDamage); // same question stays up
        }

        context.CenterHitbox();
    }

    // corner order matches Labels: 0 TL, 1 TR, 2 BL, 3 BR
    public static Rectangle ButtonRect(Rectangle box, int index)
    {
        bool right  = index == 1 || index == 3;
        bool bottom = index >= 2;

        return new Rectangle(
            right  ? box.Right  - ButtonPad - ButtonWidth  : box.Left + ButtonPad,
            bottom ? box.Bottom - ButtonPad - ButtonHeight : box.Top  + ButtonPad,
            ButtonWidth, ButtonHeight);
    }

    // the pattern draws its own buttons, DodgePhase just hands it the batch
    public void DrawUi(SpriteBatch spriteBatch, Texture2D pixel, SpriteFont font, Rectangle box)
    {
        if (IsIntro || IsDone) return;

        for (int i = 0; i < Labels.Length; i++)
        {
            Rectangle r = ButtonRect(box, i);
            DrawBorder(spriteBatch, pixel, r);

            Vector2 size = font.MeasureString(Labels[i]) * LabelScale;
            var pos = new Vector2(
                r.Center.X - size.X / 2f,
                r.Center.Y - size.Y / 2f);

            spriteBatch.DrawString(font, Labels[i], pos, ButtonColor,
                0f, Vector2.Zero, LabelScale, SpriteEffects.None, 0f);
        }
    }

    private static void DrawBorder(SpriteBatch spriteBatch, Texture2D pixel, Rectangle r)
    {
        int t = BorderThickness;

        spriteBatch.Draw(pixel, new Rectangle(r.X, r.Y, r.Width, t), ButtonColor);
        spriteBatch.Draw(pixel, new Rectangle(r.X, r.Bottom - t, r.Width, t), ButtonColor);
        spriteBatch.Draw(pixel, new Rectangle(r.X, r.Y, t, r.Height), ButtonColor);
        spriteBatch.Draw(pixel, new Rectangle(r.Right - t, r.Y, t, r.Height), ButtonColor);
    }
}
