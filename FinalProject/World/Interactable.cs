namespace FinalProject.World
{
    // A single interactable placed on a map's "Interactables" object layer in
    // Tiled — an object with a custom "Text" property. Walk up, face it, press
    // the interact button, and its Text opens in the DialogueBox.
    //
    // TileMinX/Y..TileMaxX/Y describe the tile footprint the object covers —
    // Tiled object layers are pixel-space and don't have to align to the tile
    // grid, so a placed object can span more than one tile.
    public class Interactable
    {
        public int    TileMinX { get; }
        public int    TileMinY { get; }
        public int    TileMaxX { get; }
        public int    TileMaxY { get; }
        public string Text     { get; }

        // What the interact button does here, from the object's "Action"
        // property. Empty is the default — show Text in the dialogue box, which
        // is what every sign wants. Anything else is dispatched by
        // OverworldState.TryInteract.
        public string Action { get; }

        // Only read when Action is "warp": where the door leads, and the tile
        // the player lands on once there.
        public string TargetMap { get; }
        public int    SpawnX    { get; }
        public int    SpawnY    { get; }

        public Interactable(int tileMinX, int tileMinY, int tileMaxX, int tileMaxY,
                            string text, string action = "",
                            string targetMap = null, int spawnX = 0, int spawnY = 0)
        {
            TileMinX  = tileMinX;
            TileMinY  = tileMinY;
            TileMaxX  = tileMaxX;
            TileMaxY  = tileMaxY;
            Text      = text;
            Action    = action ?? "";
            TargetMap = targetMap;
            SpawnX    = spawnX;
            SpawnY    = spawnY;
        }

        public bool ContainsTile(int tileX, int tileY)
            => tileX >= TileMinX && tileX <= TileMaxX
            && tileY >= TileMinY && tileY <= TileMaxY;
    }
}
