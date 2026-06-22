namespace FinalProject.World
{
    // Defines an exit on a map edge.
    // Fires when the player walks toward the edge in the given direction
    // while their position falls within [TileMin, TileMax].
    public class MapTransition
    {
        public string Direction { get; set; }  // "Up", "Down", "Left", "Right"
        public int    TileMin   { get; set; }  // first tile of the opening (inclusive)
        public int    TileMax   { get; set; }  // last tile of the opening (inclusive)
        public string TargetMap { get; set; }
        public int    SpawnX    { get; set; }
        public int    SpawnY    { get; set; }
    }
}
