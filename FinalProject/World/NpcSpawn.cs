namespace FinalProject.World
{
    // A stationary NPC placed on a map's "NPCs" object layer in Tiled — an
    // object with "Id" and (optionally) "Sprite"/"Scale" custom properties. Id
    // doubles as the RouteTracker key and the Teachers/{id}.json filename, so
    // winning or sparing its fight is what makes it disappear from the map for
    // good.
    public class NpcSpawn
    {
        public int    TileX      { get; }
        public int    TileY      { get; }
        public string Id         { get; }
        public string SpriteName { get; }

        // multiplies the sprite's drawn size. overworld art is authored at
        // whatever size suited the drawing, but the maps here are 20x20 tiles —
        // this is what brings an oversized sprite down to standing height
        // without re-exporting the png
        public float Scale { get; }

        public NpcSpawn(int tileX, int tileY, string id, string spriteName, float scale = 1f)
        {
            TileX      = tileX;
            TileY      = tileY;
            Id         = id;
            SpriteName = spriteName;
            Scale      = scale <= 0f ? 1f : scale;
        }
    }
}
