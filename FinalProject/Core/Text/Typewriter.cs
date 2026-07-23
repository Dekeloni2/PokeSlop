using System;
using FinalProject.Core.Audio;

namespace FinalProject.Core.Text
{
    // reveals text one character at a time, classic RPG dialogue effect.
    // (DialogueBox has its own older inline version of this)
    public class Typewriter
    {
        private const float CharsPerSecond = 40f;

        private string _text = "";
        private float  _timer;
        private int    _visibleChars;

        // which blip plays as characters reveal — defaults to the normal beep
        private readonly string _beepSound;

        public Typewriter(string beepSound = null)
            => _beepSound = beepSound ?? SoundManager.TextBeepName;

        public bool   IsFullyShown => _visibleChars >= _text.Length;
        public string VisibleText  => _text.Substring(0, _visibleChars);
        public string FullText     => _text;

        public void SetText(string text)
        {
            _text         = text ?? "";
            _timer        = 0f;
            _visibleChars = 0;
        }

        public void Update(float dt)
        {
            if (IsFullyShown) return;

            _timer += dt;
            int revealed = Math.Min((int)(_timer * CharsPerSecond), _text.Length);

            // blip once per frame for each newly revealed non-space character
            if (revealed > _visibleChars)
            {
                SoundManager.PlayTextBeep(_beepSound, _text, _visibleChars, revealed);
                _visibleChars = revealed;
            }
        }

        public void SkipToEnd() => _visibleChars = _text.Length;
    }
}
