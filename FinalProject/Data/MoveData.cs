namespace FinalProject.Data
{
    // Immutable blueprint for a move — shared across all users of that move.
    public class MoveData
    {
        public string      Name        { get; }
        public int         Power       { get; }    // 0 = status move, no damage
        public int         Accuracy    { get; }    // 0-100
        public string      Description { get; }

        public bool IsStatusMove => Power == 0;

        public MoveData(string name, int power, int accuracy, string description)
        {
            Name        = name;
            Power       = power;
            Accuracy    = accuracy;
            Description = description;
        }
    }
}
