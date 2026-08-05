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
    // music and sfx used to be one combined "Sound Volume" knob — split so
    // either can be turned down (or off) without taking the other with it
    public static int MusicVolume { get; private set; } = 80; // 0 to 100
    public static int SfxVolume   { get; private set; } = 80; // 0 to 100
    public static int TargetFps   { get; private set; } = 60; // 30 or 60

    public static void SetMusicVolume(int volume)
    {
        MusicVolume = MathHelper.Clamp(volume, 0, 100);
        SoundManager.MusicVolume = MusicVolume / 100f;

        // re-applies against whatever's already playing, without resetting
        // its own per-track volume (see SoundManager.RefreshMusicVolume)
        SoundManager.RefreshMusicVolume();
    }

    public static void SetSfxVolume(int volume)
    {
        SfxVolume = MathHelper.Clamp(volume, 0, 100);
        SoundManager.SfxVolume = SfxVolume / 100f;
    }

    public static void SetTargetFps(Game game, int fps)
    {
        TargetFps = fps;
        game.IsFixedTimeStep = true;
        game.TargetElapsedTime = TimeSpan.FromSeconds(1.0 / TargetFps);
    }

    // Everything draws at the fixed WindowWidth x WindowHeight resolution
    // into an offscreen target (see Game1.Draw), and this is what places
    // that target inside whatever the real back buffer size is — windowed
    // at exactly 720x480 needs no scaling, fullscreen (or any other back
    // buffer size) gets scaled up as far as it can while keeping the 3:2
    // ratio intact, centred with letterbox/pillarbox bars rather than
    // stretched or cropped.
    public static Rectangle FitToScreen(int targetWidth, int targetHeight)
    {
        float scale = Math.Min(targetWidth / (float)WindowWidth, targetHeight / (float)WindowHeight);

        int w = (int)(WindowWidth  * scale);
        int h = (int)(WindowHeight * scale);

        return new Rectangle((targetWidth - w) / 2, (targetHeight - h) / 2, w, h);
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