using System.Collections.Generic;

namespace FinalProject.Data
{
    // Immutable blueprint for a species — shared across all instances of that creature.
    // Individual creatures in battle are represented by a separate Creature instance class.
    public class TeacherStats
    {
        public string      Name          { get; }
        public string      SpriteName    { get; }

        public int BaseHp    { get; }
        public int BaseAtk   { get; }

        public TeacherStats(
            string name, string spriteName, int baseHp, int baseAtk)
        {
            Name          = name;
            SpriteName    = spriteName;
            BaseHp        = baseHp;
            BaseAtk       = baseAtk;
        }
    }
}
