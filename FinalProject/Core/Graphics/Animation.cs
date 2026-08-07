namespace FinalProject.Core.Graphics;

using System;
using Microsoft.Xna.Framework;


public class Animation : Sprite
{
    private double _totalTime;
    private int    _samples = 60;
    private int    _x;
    private int    _y;
    private bool   _isLooping = true;
    private bool   _isAnimating;

    // the frame the caller last asked us to show — exposed so a caller that
    // draws through its own pipeline (e.g. DamageDisplay, which scales the
    // slash to the teacher's height instead of using Draw() below) can still
    // read what "now" looks like.
    public Rectangle? CurrentFrame => SourceRect;

    public Animation(string spriteName) : base(spriteName)
    {
        // frame (0,0) up front, not left null until the first Update — Update
        // reads SourceRect.Value unconditionally (see Sprite.Update), so
        // anyone who draws before the first PlayAnimation()/Update() pair
        // would otherwise hit a null Rectangle? deref.
        Reset();
    }

    public void PlayAnimation(bool isLooping = true, int samples = 60)
    {
        _isLooping = isLooping;
        _samples   = samples;
        Reset();
        _isAnimating = true;
    }

    // plays the whole sheet through once over durationSeconds, then holds on
    // the last frame — the shape DamageDisplay's slash needs. Frame count
    // comes from the sheet itself, so this stays in sync if the art changes.
    public void PlayOnce(float durationSeconds)
    {
        int totalFrames = Math.Max(1, Spritesheet.Columns * Spritesheet.Rows);
        int samples = durationSeconds > 0f
            ? (int)MathF.Round(totalFrames / durationSeconds)
            : totalFrames;

        PlayAnimation(isLooping: false, samples: Math.Max(1, samples));
    }

    public void StopAnimation() => Reset();

    public void PauseAnimation() => _isAnimating = false;

    public void ResumeAnimation() => _isAnimating = true;

    private void Reset()
    {
        _isAnimating = false;
        _x = _y = 0;
        _totalTime = 0;
        SourceRect  = Spritesheet[_x, _y];
        DestRect    = GetDestRect(SourceRect);
        RecalculateOrigin();
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
