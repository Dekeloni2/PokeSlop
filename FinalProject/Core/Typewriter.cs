using System;

namespace FinalProject.Core
{
    // reveals text one character at a time, classic RPG dialogue effect.
    // (DialogueBox has its own older inline version of this)
    public class Typewriter
    {
        private const float CharsPerSecond = 40f;

        private string _text = "";
        private float  _timer;
        private int    _visibleChars;

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
            _visibleChars = Math.Min((int)(_timer * CharsPerSecond), _text.Length);
        }

        public void SkipToEnd() => _visibleChars = _text.Length;
    }
}
