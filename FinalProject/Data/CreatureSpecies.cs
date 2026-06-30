using System.Collections.Generic;

namespace FinalProject.Data
{
    // Immutable blueprint for a species — shared across all instances of that creature.
    // Individual creatures in battle are represented by a separate Creature instance class.
    public class CreatureSpecies
    {
        public int         Id            { get; }
        public string      Name          { get; }
        public CreatureType Type         { get; }
        public string      SpriteName    { get; }
        public int         EvolvesIntoId { get; }  // -1 means final form

        public int BaseHp    { get; }
        public int BaseAtk   { get; }
        public int BaseDef   { get; }
        public int BaseSpAtk { get; }
        public int BaseSpDef { get; }
        public int BaseSpeed { get; }

        // Moves learned at level up: (level, move name)
        public IReadOnlyList<(int Level, string MoveName)> Learnset { get; }

        public bool IsFinalForm => EvolvesIntoId == -1;

        public CreatureSpecies(
            int id, string name, CreatureType type, string spriteName, int evolvesIntoId,
            int baseHp, int baseAtk, int baseDef, int baseSpAtk, int baseSpDef, int baseSpeed,
            IReadOnlyList<(int, string)> learnset)
        {
            Id            = id;
            Name          = name;
            Type          = type;
            SpriteName    = spriteName;
            EvolvesIntoId = evolvesIntoId;
            BaseHp        = baseHp;
            BaseAtk       = baseAtk;
            BaseDef       = baseDef;
            BaseSpAtk     = baseSpAtk;
            BaseSpDef     = baseSpDef;
            BaseSpeed     = baseSpeed;
            Learnset      = learnset;
        }
    }
}
