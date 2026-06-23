using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace FinalProject.Data
{
    // Central registry of every creature species in the game.
    // Data lives in Content/Data/species.json — add new species there, not here.
    // Look up a species by ID: SpeciesRegistry.Get(1) → Embersaur
    public static class SpeciesRegistry
    {
        private static readonly Dictionary<int, CreatureSpecies> _all = new();

        public static CreatureSpecies Get(int id) => _all[id];
        public static IEnumerable<CreatureSpecies> All => _all.Values;

        // Call once at startup before any species are needed.
        // jsonPath: absolute path to species.json
        public static void Load(string jsonPath)
        {
            _all.Clear();

            string json = File.ReadAllText(jsonPath);
            using JsonDocument doc = JsonDocument.Parse(json);

            foreach (JsonElement el in doc.RootElement.EnumerateArray())
            {
                int    id            = el.GetProperty("id").GetInt32();
                string name          = el.GetProperty("name").GetString();
                string typeStr       = el.GetProperty("type").GetString();
                string spriteName    = el.GetProperty("spriteName").GetString();
                int    evolvesIntoId = el.GetProperty("evolvesIntoId").GetInt32();
                int    baseHp        = el.GetProperty("baseHp").GetInt32();
                int    baseAtk       = el.GetProperty("baseAtk").GetInt32();
                int    baseDef       = el.GetProperty("baseDef").GetInt32();
                int    baseSpAtk     = el.GetProperty("baseSpAtk").GetInt32();
                int    baseSpDef     = el.GetProperty("baseSpDef").GetInt32();
                int    baseSpeed     = el.GetProperty("baseSpeed").GetInt32();

                var type = Enum.Parse<CreatureType>(typeStr);

                var learnset = new List<(int Level, string MoveName)>();
                foreach (JsonElement entry in el.GetProperty("learnset").EnumerateArray())
                {
                    learnset.Add((
                        entry.GetProperty("level").GetInt32(),
                        entry.GetProperty("moveName").GetString()
                    ));
                }

                _all[id] = new CreatureSpecies(
                    id, name, type, spriteName, evolvesIntoId,
                    baseHp, baseAtk, baseDef, baseSpAtk, baseSpDef, baseSpeed,
                    learnset);
            }
        }
    }
}
