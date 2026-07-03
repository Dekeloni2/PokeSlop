// World/Interactable.cs
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

        public Interactable(int tileMinX, int tileMinY, int tileMaxX, int tileMaxY, string text)
        {
            TileMinX = tileMinX;
            TileMinY = tileMinY;
            TileMaxX = tileMaxX;
            TileMaxY = tileMaxY;
            Text     = text;
        }

        public bool ContainsTile(int tileX, int tileY)
            => tileX >= TileMinX && tileX <= TileMaxX
            && tileY >= TileMinY && tileY <= TileMaxY;
    }
}
