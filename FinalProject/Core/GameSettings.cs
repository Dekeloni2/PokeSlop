using System;
using Microsoft.Xna.Framework;
using FinalProject.Core.Audio;

namespace FinalProject.Core;

public static class GameSettings
{
    public const string GameTitle = "TiltanTale";
    public const int  WindowWidth  = 720;
    public const int  WindowHeight = 480;

    public const int TileSize = 20;
    
    public static int TilesWide => WindowWidth / TileSize;
    public static int TilesTall => WindowHeight / TileSize;
    
    public const float Zoom = 2f;

    // undertale moves 2px per frame at 30fps, so 60px/sec. ours is time based
    // rather than per frame, so it stays the same at any framerate
    public const float PlayerSpeed = 100f;
    
    // ── Settings Data & Controls ───────────────────────────────────────
    public static int MasterVolume { get; private set; } = 80; // 0 to 100
    public static int TargetFps { get; private set; } = 60;    // 30 or 60

    public static void SetVolume(int volume)
    {
        MasterVolume = MathHelper.Clamp(volume, 0, 100);
        float targetVolume = MasterVolume / 100f;

        // Tell SoundManager the new volumes!
        SoundManager.SfxVolume   = targetVolume;
        SoundManager.MusicVolume = targetVolume;

        // Immediately apply to the live song player
        SoundManager.SetMusicVolume(1f);
        
    }

    public static void SetTargetFps(Game game, int fps)
    {
        TargetFps = fps;
        game.IsFixedTimeStep = true;
        game.TargetElapsedTime = TimeSpan.FromSeconds(1.0 / TargetFps);
    }
    
    // ── Battle: attack minigame ─────────────────────────────────────────
    public const float AttackBarSpeed      = 480f; // px/sec sweep across the target zone
    public const float AttackFlashSeconds  = 0.7f; // how long the bar flashes after a hit
    public const float AttackFlashInterval = 0.1f; // sec per white/black flash frame

    // ── Battle: dodge phase ─────────────────────────────────────────────
    public const int   DodgeBoxDefaultSize  = 200;  // px, base square arena side length
    public const int   DodgeHitboxSize      = 8;    // px, player-controlled square
    public const float DodgeHitboxSpeed     = 100f; // px/sec
    public const int   DodgeProjectileSize  = 6;    // px, square
    public const float DodgeProjectileSpeed = 90f;  // px/sec
    public const float DodgeSpawnInterval   = 0.35f; // sec between spawns within a burst
    public const int   DodgeBoundsMargin    = 16;   // px outside the current box before a projectile expires
}