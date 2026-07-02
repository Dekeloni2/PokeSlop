namespace FinalProject.Data
{
    // Immutable blueprint for a move — shared across all users of that move.
    public class MoveData
    {
        public string      Name        { get; }
        public int         Power       { get; }    // 0 = status move, no damage
        public string      Description { get; }

        public MoveData(string name, int power, string description)
        {
            Name        = name;
            Power       = power;
            Description = description;
        }
    }
}
