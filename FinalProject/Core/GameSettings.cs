namespace FinalProject.Core;

public static class GameSettings
{
    public const string GameTitle = "TiltanTale";
    public const int  WindowWidth  = 720;
    public const int  WindowHeight = 480;

    public const int TileSize = 16;
    
    public static int TilesWide => WindowWidth / TileSize;
    public static int TilesTall => WindowHeight / TileSize;

    // How many screen pixels each world pixel occupies — 2 = GBA 2× look
    public const float Zoom = 2f;

    public const float PlayerSpeed = 64f;

    public const float WildEncounterChance = 0.10f;
}