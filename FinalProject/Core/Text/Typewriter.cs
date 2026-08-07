using System;
using FinalProject.Core.Audio;

namespace FinalProject.Core.Text
{
    // reveals text one character at a time, classic RPG dialogue effect.
    // (DialogueBox has its own older inline version of this)
    public class Typewriter
    {
        private const float DefaultCharsPerSecond = 40f;

        private readonly float _charsPerSecond;

        private string _text = "";
        private float  _timer;
        private int    _visibleChars;

        // which blip plays as characters reveal — defaults to the normal beep
        public string BeepSound { get; set; }

        public Typewriter(string beepSound = null, float charsPerSecond = DefaultCharsPerSecond)
        {
            BeepSound       = beepSound ?? SoundManager.TextBeepName;
            _charsPerSecond = charsPerSecond;
        }

        public bool   IsFullyShown => _visibleChars >= _text.Length;
        public string VisibleText  => _text.Substring(0, _visibleChars);

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
            int revealed = Math.Min((int)(_timer * _charsPerSecond), _text.Length);

            // blip once per frame for each newly revealed non-space character
            if (revealed > _visibleChars)
            {
                SoundManager.PlayTextBeep(BeepSound ?? SoundManager.TextBeepName, _text, _visibleChars, revealed);
                _visibleChars = revealed;
            }
        }

        public void SkipToEnd() => _visibleChars = _text.Length;
    }
}
