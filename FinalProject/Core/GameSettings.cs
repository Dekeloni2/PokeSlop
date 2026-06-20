namespace FinalProject.Core;

public static class GameSettings
{
    public const string GameTitle = "PokeSlop";
    public const int  WindowWidth = 800;
    public const int  WindowHeight = 600;

    public const int TileSize = 16;
    
    public static int TilesWide => WindowWidth / TileSize;
    public static int TilesTall => WindowHeight / TileSize;

    public const float PlayerSpeed = 120f;

    public const float WildEncounterChance = 0.10f;
}