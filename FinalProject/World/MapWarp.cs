using System.Collections.Generic;

namespace FinalProject.World
{
    // An on-step warp trigger: when the player stands on any tile in Tiles,
    // they are moved to (SpawnX, SpawnY) on TargetMap. Unlike MapTransition
    // (which fires at map edges when walking off the boundary), this triggers
    // from interior tiles and needs no facing direction — e.g. stepping onto
    // the elevator's floor tiles to ride into the elevator room.
    public class MapWarp
    {
        public List<WarpTile> Tiles { get; set; } = new();
        public string TargetMap { get; set; }
        public int    SpawnX    { get; set; }
        public int    SpawnY    { get; set; }

        public bool ContainsTile(int tileX, int tileY)
        {
            foreach (WarpTile t in Tiles)
                if (t.X == tileX && t.Y == tileY)
                    return true;
            return false;
        }
    }

    // A single trigger tile. Kept as a class with int properties so it maps
    // cleanly from JSON — XNA's Point exposes X/Y as fields, which
    // System.Text.Json skips by default.
    public class WarpTile
    {
        public int X { get; set; }
        public int Y { get; set; }
    }
}
