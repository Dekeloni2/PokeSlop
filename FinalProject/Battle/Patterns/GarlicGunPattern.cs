using System;
using Microsoft.Xna.Framework;

namespace FinalProject.Battle.Patterns;


public class GarlicGunPattern : IBulletPattern
{
    public float Duration => 30f;

    private enum State { Charging, Firing, Vanishing, Done }

    private State _state;
    private float _timer;   // time spent in the current state

    private float _beamCenterY;  // world Y the beam is centered on (tracks player)
    private bool  _fromLeft;     // which side Vegeta stands on
    private bool  _isFeint;      // this cycle charges but never fires (a fake-out)

    private Beam _beam;          // the live beam during Firing

    // warning telegraph flash
    private int   _warningFrame;
    private float _warningFrameTimer;

    // base cycle length, before the end-of-turn speed ramp is applied
    private const float BaseChargeSeconds = 0.9f;
    private const float BaseFireSeconds   = 1.1f;

    // he shoots faster the closer the turn gets to its end: the cycle length
    // scales from StartSpeedMul (slow) to EndSpeedMul (fast) as Elapsed
    // progresses through Duration.
    private const float StartSpeedMul = 1.0f;
    private const float EndSpeedMul   = 0.22f;
    private float _speedMul = StartSpeedMul;

    private const float FeintChance         = 0.1f;  // fraction of charges that are fake-outs
    private const float WarningFlashSeconds = 0.08f; // per warning frame
    private const int   BeamThickness       = 30;
    private const float BeamExtendSeconds   = 0.12f; // beam sweep-in time

    // Vegeta's sprite is a one-shot per phase: each phase shows its first frame
    // briefly then holds the second. FirstFrameSeconds is how long the first
    // frame stays up before holding.
    private const float FirstFrameSeconds = 0.12f;
    // the flip-out pose (frame 4) holds this long before he teleports away
    private const float VanishSeconds     = 0.22f;

    // exposed for DodgePhase to draw Vegeta + the warning telegraph
    public bool IsCharging  => _state == State.Charging;
    public bool IsFiring    => _state == State.Firing;
    public bool IsVanishing => _state == State.Vanishing;
    public bool FromLeft    => _fromLeft;
    public int  WarningFrame => _warningFrame;

    // 0→1 through the charge phase, used to reveal the warning markers one by one
    public float ChargeProgress => _state == State.Charging
        ? MathHelper.Clamp(_timer / (BaseChargeSeconds * _speedMul), 0f, 1f)
        : 1f;

    // charge: frame 0 then hold 1 · fire: frame 2 then hold 3 · vanish: frame 4
    public int VegetaFrame => _state switch
    {
        State.Charging  => _timer < FirstFrameSeconds ? 0 : 1,
        State.Firing    => _timer < FirstFrameSeconds ? 2 : 3,
        State.Vanishing => 4,
        _               => 1
    };

    // the strip the beam occupies — centered on the player's tracked Y, and
    // where the warning telegraph is drawn.
    public Rectangle BeamStrip(Rectangle box)
    {
        int top = (int)(_beamCenterY - BeamThickness / 2f);
        return new Rectangle(box.Left, top, box.Width, BeamThickness);
    }

    public void Start(DodgeContext context)
    {
        BeginCharge(context);
    }

    public void Update(GameTime gameTime, DodgeContext context)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _timer += dt;

        // ramp the cycle speed up as the turn nears its end. Squaring the
        // progress eases it in — he stays slow through the early/mid turn, then
        // the speedup concentrates in the final stretch for a frantic finish.
        float progress = MathHelper.Clamp(context.Elapsed / Duration, 0f, 1f);
        _speedMul = MathHelper.Lerp(StartSpeedMul, EndSpeedMul, progress * progress);

        switch (_state)
        {
            case State.Charging:
                _warningFrameTimer += dt;
                if (_warningFrameTimer >= WarningFlashSeconds)
                {
                    _warningFrameTimer -= WarningFlashSeconds;
                    _warningFrame = (_warningFrame + 1) % 3;
                }

                if (_timer >= BaseChargeSeconds * _speedMul)
                {
                    if (_isFeint)
                        BeginVanish(context); // charged up, but never fires
                    else
                        BeginFire(context);
                }
                break;

            case State.Firing:
                // the beam sweeps across from Vegeta's side, then holds full width
                if (_beam != null)
                    _beam.Bounds = BeamRect(context.CurrentBox, _timer);

                if (_timer >= BaseFireSeconds * _speedMul)
                    BeginVanish(context);
                break;

            case State.Vanishing:
                if (_timer >= VanishSeconds)
                {
                    if (context.Elapsed < Duration)
                        BeginCharge(context);
                    else
                        _state = State.Done;
                }
                break;

            case State.Done:
                break;
        }
    }

    private void BeginCharge(DodgeContext context)
    {
        _state = State.Charging;
        _timer = 0f;
        _warningFrame = 0;
        _warningFrameTimer = 0f;

        _fromLeft = Random.Shared.Next(2) == 0;
        _isFeint  = Random.Shared.NextDouble() < FeintChance;

        // aim the beam straight at the player's current Y — they have to move
        // clear of it during the charge to dodge. Clamped so the strip stays
        // fully inside the box.
        float half = BeamThickness / 2f;
        _beamCenterY = MathHelper.Clamp(context.HitboxPosition.Y,
            context.CurrentBox.Top + half, context.CurrentBox.Bottom - half);
    }

    private void BeginFire(DodgeContext context)
    {
        _state = State.Firing;
        _timer = 0f;
        _beam = context.AddBeam(BeamRect(context.CurrentBox, 0f));
    }

    private void BeginVanish(DodgeContext context)
    {
        EndFire(context);
        _state = State.Vanishing;
        _timer = 0f;
    }

    private void EndFire(DodgeContext context)
    {
        if (_beam != null)
        {
            context.RemoveBeam(_beam);
            _beam = null;
        }
    }

    // the beam grows from Vegeta's side across the box over BeamExtendSeconds,
    // so it reads as a sweeping wave rather than snapping to full width
    private Rectangle BeamRect(Rectangle box, float fireElapsed)
    {
        Rectangle strip = BeamStrip(box);
        float t = MathHelper.Clamp(fireElapsed / BeamExtendSeconds, 0f, 1f);
        int width = (int)(box.Width * t);
        int x = _fromLeft ? box.Left : box.Right - width;
        return new Rectangle(x, strip.Y, width, strip.Height);
    }
}
