namespace FinalProject.Core.Graphics;

using Microsoft.Xna.Framework;


public class Animation : Sprite
{
    private double _totalTime;
    private int    _samples = 60;
    private int    _x;
    private int    _y;
    private bool   _isLooping = true;
    private bool   _isAnimating;

    public Animation(string spriteName) : base(spriteName)
    {
    }

    public void PlayAnimation(bool isLooping = true, int samples = 60)
    {
        _isLooping = isLooping;
        _samples   = samples;
        Reset();
        _isAnimating = true;
    }

    public void StopAnimation() => Reset();

    public void PauseAnimation() => _isAnimating = false;

    public void ResumeAnimation() => _isAnimating = true;

    private void Reset()
    {
        _isAnimating = false;
        _x = _y = 0;
        _totalTime = 0;
    }

    public override void Update(GameTime gameTime)
    {
        if (!_isAnimating) return;

        if (CanMoveFrame(gameTime))
            MoveFrame();

        base.Update(gameTime);
    }

    private bool CanMoveFrame(GameTime gameTime)
    {
        _totalTime += gameTime.ElapsedGameTime.TotalSeconds;
        return _totalTime >= 1.0 / _samples;
    }

    private void MoveFrame()
    {
        _totalTime = 0;
        _x++;

        if (_x == Spritesheet.Columns)
        {
            _x = 0;
            _y++;
            if (_y == Spritesheet.Rows)
            {
                if (_isLooping)
                {
                    _x = 0;
                    _y = 0;
                }
                else
                {
                    _x = Spritesheet.Columns - 1;
                    _y = Spritesheet.Rows - 1;
                }
            }
        }

        SourceRect = Spritesheet[_x, _y];
        DestRect   = GetDestRect(SourceRect);
    }
}
