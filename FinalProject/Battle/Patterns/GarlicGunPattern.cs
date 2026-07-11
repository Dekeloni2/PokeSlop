using Microsoft.Xna.Framework;
using System;
using FinalProject.Core;

namespace FinalProject.Battle.Patterns;

public class GarlicGunPattern : IBulletPattern
{
    public bool IsWarning => _stateA == BlasterState.Warning || _stateB == BlasterState.Warning;

    public float Duration => 10f;
    
    private enum BlasterState { Idle, Warning, Firing }
    
    // blaster A slot
    private BlasterState _stateA = BlasterState.Idle; 
    private int _laneA = -1;
    private float _timerA;
    private float _shotTimerA;

    // blaster B slot
    private BlasterState _stateB = BlasterState.Idle;
    private int _laneB = -1;
    private float _timerB;
    private float _shotTimerB;
    
    public int CurrentWarningLane
    {
        get
        {
            if (_stateA == BlasterState.Warning)
                return _laneA;

            if (_stateB == BlasterState.Warning)
                return _laneB;

            return -1;
        }
    }
    
    // spawn settings
    private float _globalSpawnTimer;
    private float _spawnInterval = 1.0f; // time between each blaster slot
    private int _lastChosenLane = -1;
    
    // duration settings
    private const float WarningDuration = 0.8f;
    private const float FireDuration = 0.6f;
    
    private const float LaserFireRate = 0.04f; // time between each cube (lower = denser beam)
    private const float LaserSpeed = 650f; 
    
    public void Start(DodgeContext context)
    {
        _stateA = BlasterState.Idle;
        _stateB = BlasterState.Idle;
        _globalSpawnTimer = 0f;
        _lastChosenLane = -1;
    }

    public void Update(GameTime gameTime, DodgeContext context)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        Rectangle box = context.CurrentBox;

        if (context.Elapsed >= Duration)
        {
            if (_stateA == BlasterState.Idle && _stateB == BlasterState.Idle) return;
        }
        else
        {
            _globalSpawnTimer += dt;
            if (_globalSpawnTimer >= _spawnInterval)
            {
                _globalSpawnTimer = 0f;
                TryTriggerNextBlaster();
            }
        }
        
        // state manager
        ProcessSlot(dt, context, box, ref _stateA, ref _timerA, ref _shotTimerA, _laneA);
        ProcessSlot(dt, context, box, ref _stateB, ref _timerB, ref _shotTimerB, _laneB);
    }
    
    private void TryTriggerNextBlaster()
    {
        // prevents picking the same line twice in a row
        int chosenLane;
        do
        {
            chosenLane = Random.Shared.Next(3);
        } 
        while (chosenLane == _lastChosenLane);
        _lastChosenLane = chosenLane;

        // activate which slot is currently idle
        if (_stateA == BlasterState.Idle)
        {
            _laneA = chosenLane;
            _timerA = 0f;
            _stateA = BlasterState.Warning; // change state
        }
        else if (_stateB == BlasterState.Idle)
        {
            _laneB = chosenLane;
            _timerB = 0f;
            _stateB = BlasterState.Warning; // change state
        }
    }

    private void ProcessSlot(float dt, DodgeContext context, Rectangle box, ref BlasterState state, ref float timer, ref float shotTimer, int lane)
    {
        if (state == BlasterState.Idle) return;

        timer += dt;
        float laneHeight = box.Height / 3f;
        
        // centered vertically within the lane tracking bounds
        float laneCenterY = box.Top + (lane * laneHeight) + (laneHeight / 2f);

        // position the sprite nicely off-screen to the left
        Vector2 blasterPos = new Vector2(box.Left - 40f, laneCenterY);
        
        Vector2 streamSpawnOrigin = new Vector2(box.Left, laneCenterY);
        Vector2 laserVelocity = new Vector2(LaserSpeed, 0f);

        switch (state)
        {
            case BlasterState.Warning:
                
                if (timer >= WarningDuration)
                {
                    timer = 0f;
                    shotTimer = 0f;
                    state = BlasterState.Firing;
                }
                break;

            case BlasterState.Firing:

                shotTimer += dt;
                
                while (shotTimer >= LaserFireRate)
                {
                    context.SpawnProjectile(streamSpawnOrigin, laserVelocity, ProjectileType.Laser);
                    shotTimer -= LaserFireRate;
                }

                if (timer >= FireDuration)
                {
                    timer = 0f;
                    shotTimer = 0f;
                    state = BlasterState.Idle;
                }
                break;
        }
    }
}