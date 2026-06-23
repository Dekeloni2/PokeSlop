using Microsoft.Xna.Framework;

namespace FinalProject.Core
{
    public static class DirectionExtensions
    {
        // Returns the tile coordinate one step in the given direction from 'from'.
        public static Point GetNeighbour(this Direction direction, Point from) => direction switch
        {
            Direction.Up    => new Point(from.X,     from.Y - 1),
            Direction.Down  => new Point(from.X,     from.Y + 1),
            Direction.Left  => new Point(from.X - 1, from.Y),
            Direction.Right => new Point(from.X + 1, from.Y),
            _               => from
        };
    }
}
