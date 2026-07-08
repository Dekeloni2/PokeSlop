using Microsoft.Xna.Framework;

namespace FinalProject.Battle
{
    // a Rectangle that animates towards a target rect over time. Used for
    // the battle box transitions and mid-attack arena resizes.
    public class TweeningBox
    {
        public Rectangle Current { get; private set; }
        public bool IsAnimating => _duration > 0f;

        private Rectangle _from;
        private Rectangle _to;
        private float _elapsed;
        private float _duration;

        public TweeningBox(Rectangle initial) => Current = initial;

        public void ResizeTo(Rectangle target, float overSeconds)
        {
            if (overSeconds <= 0f)
            {
                Current = target;
                _duration = 0f;
                return;
            }

            _from     = Current;
            _to       = target;
            _elapsed  = 0f;
            _duration = overSeconds;
        }

        public void Update(float deltaTimeSeconds)
        {
            if (_duration <= 0f) return;

            _elapsed += deltaTimeSeconds;
            float lerpProgress = MathHelper.Clamp(_elapsed / _duration, 0f, 1f);
            Current = Lerp(_from, _to, lerpProgress);

            if (lerpProgress >= 1f)
                _duration = 0f;
        }

        private static Rectangle Lerp(Rectangle a, Rectangle b, float lerpProgress) => new Rectangle(
            (int)MathHelper.Lerp(a.X,      b.X,      lerpProgress),
            (int)MathHelper.Lerp(a.Y,      b.Y,      lerpProgress),
            (int)MathHelper.Lerp(a.Width,  b.Width,  lerpProgress),
            (int)MathHelper.Lerp(a.Height, b.Height, lerpProgress));
    }
}
