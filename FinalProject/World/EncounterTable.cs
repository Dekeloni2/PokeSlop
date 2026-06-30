using System;
using System.Collections.Generic;
using FinalProject.Battle;
using FinalProject.Data;

namespace FinalProject.World
{
    public class EncounterEntry
    {
        public int SpeciesId { get; set; }
        public int MinLevel  { get; set; }
        public int MaxLevel  { get; set; }
        public int Weight    { get; set; }
    }

    public class EncounterTable
    {
        private readonly List<EncounterEntry> _entries;
        private readonly int _totalWeight;

        public EncounterTable(List<EncounterEntry> entries)
        {
            _entries     = entries;
            _totalWeight = 0;
            foreach (var e in entries) _totalWeight += e.Weight;
        }

        // Picks a random species weighted by Weight, then picks a random level in range
        public Creature SpawnRandom(Random rng)
        {
            int roll       = rng.Next(_totalWeight);
            int cumulative = 0;

            foreach (var entry in _entries)
            {
                cumulative += entry.Weight;
                if (roll < cumulative)
                {
                    int level = rng.Next(entry.MinLevel, entry.MaxLevel + 1);
                    return new Creature(SpeciesRegistry.Get(entry.SpeciesId), level);
                }
            }

            // Fallback — should never reach here
            var last = _entries[_entries.Count - 1];
            return new Creature(SpeciesRegistry.Get(last.SpeciesId), last.MinLevel);
        }
    }
}
