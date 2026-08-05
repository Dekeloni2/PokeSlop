using Microsoft.Xna.Framework;
using FinalProject.Core;

namespace FinalProject.Battle.Patterns;

// Yakir's ultimate. a callback to him spending a whole lesson failing to get one
// pixel onto the screen, so his big finish is a single pixel drifting slowly
// across the arena. it's meant to be easy, the joke is the build up, so he gets
// a proper villain announcement in front of it and a victory lap after.
// the attack ends the moment the pixel connects, or gives up after a while if
// it never does, either way he takes full credit
public class PixelPattern : IBulletPattern
{
    public float Duration => _doneTime >= 0f ? _doneTime + EndPause : 60f;

    private const float EndPause = 1.2f;

    private const float IntroSeconds   = 3.4f; // the announcement
    private const float TimeoutSeconds = 7.0f; // how long the pixel gets to land
    private const float OutroSeconds   = 2.8f; // the victory lap
    private const float HitPause       = 0.7f; // beat after it connects
    private const float PixelSpeed     = 22f;  // px/sec, deliberately slow

    // the arena tightens for the finale, and it happens on the announcement so
    // the shrink is something you watch rather than something already done.
    // BoxTop is raised enough that the bottom edge (BoxTop + BoxHeight)
    // clears the HP bar's row (y=390) with some margin, same box size
    private const int BoxWidth  = 280;
    private const int BoxHeight = 130;
    private const int BoxTop    = 250;
    private const float ShrinkSeconds = 0.7f;

    private enum Phase { Intro, Attack, Landed, Outro, Done }

    private Phase _phase = Phase.Intro;
    private float _phaseTimer;
    private float _elapsed;
    private float _doneTime = -1f;
    private bool  _spawned;
    private int   _hitsAtStart;

    private string PromptText => _phase switch
    {
        Phase.Intro => "* I see you still haven't learned anything. Prepare for my final attack!",
        Phase.Outro => "* What do you think of that!",
        _           => null, // the pixel speaks for itself
    };

    public void Start(DodgeContext context)
    {
        // no resize here on purpose, the box stays wide while he monologues
        context.SetTeacherVisible(true);
        context.SetTeacherSpeech(PromptText);
    }

    public void Update(GameTime gameTime, DodgeContext context)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _elapsed    += dt;
        _phaseTimer += dt;

        context.SetTeacherSpeech(PromptText);

        switch (_phase)
        {
            case Phase.Intro:
                if (_phaseTimer < IntroSeconds) break;

                // the walls close in as he finishes the threat
                context.ResizeBoxTo(
                    new Rectangle(GameSettings.WindowWidth / 2 - BoxWidth / 2, BoxTop,
                                  BoxWidth, BoxHeight),
                    ShrinkSeconds);

                _hitsAtStart = context.PlayerHitCount;
                Advance(Phase.Attack);
                break;

            case Phase.Attack:
                // wait out the shrink first. a pixel spawned on the old right
                // wall would be left outside the new one and expire on the spot
                if (!_spawned && _phaseTimer >= ShrinkSeconds)
                {
                    _spawned = true;

                    // one pixel, entering from the right down the middle of the
                    // arena. aiming it at the soul put it on whichever wall the
                    // shrink had just pinned the player against
                    Rectangle box = context.CurrentBox;
                    context.SpawnProjectile(
                        new Vector2(box.Right, box.Center.Y),
                        new Vector2(-PixelSpeed, 0f), ProjectileType.Pixel);
                }

                // it landed, that's all he wanted. let the hit read, then gloat
                if (_spawned && context.PlayerHitCount > _hitsAtStart)
                {
                    context.ClearProjectiles();
                    Advance(Phase.Landed);
                    break;
                }

                // it never connected, quietly retire the pixel and gloat anyway
                if (_phaseTimer >= ShrinkSeconds + TimeoutSeconds)
                {
                    context.ClearProjectiles();
                    Advance(Phase.Outro);
                }
                break;

            case Phase.Landed:
                if (_phaseTimer >= HitPause) Advance(Phase.Outro);
                break;

            case Phase.Outro:
                if (_phaseTimer >= OutroSeconds)
                {
                    _doneTime = _elapsed;
                    Advance(Phase.Done);
                }
                break;
        }
    }

    private void Advance(Phase next)
    {
        _phase      = next;
        _phaseTimer = 0f;
    }
}
