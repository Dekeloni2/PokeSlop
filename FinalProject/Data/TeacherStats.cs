using System.Collections.Generic;

namespace FinalProject.Data
{
    // Immutable blueprint for one boss Teacher. There's no species/leveling
    // system here — each Teacher is a unique, hand-defined boss, so this just
    // holds that boss's fixed stats and move list directly (no name-based
    // lookup/registry — Moves are real MoveData, not references to resolve).
    public class TeacherStats
    {
        public string      Name          { get; }
        public string      SpriteName    { get; }
        public int         BaseHp        { get; }
        public int         BaseAtk       { get; }
        public IReadOnlyList<MoveData> Moves { get; }

        public TeacherStats(
            string name, string spriteName, int baseHp, int baseAtk,
            IReadOnlyList<MoveData> moves)
        {
            Name       = name;
            SpriteName = spriteName;
            BaseHp     = baseHp;
            BaseAtk    = baseAtk;
            Moves      = moves ?? new List<MoveData>();
        }
    }
}
