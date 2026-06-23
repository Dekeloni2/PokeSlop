using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace FinalProject.Data
{
    // Central registry of every move in the game.
    // Data lives in Content/Data/moves.json — add new moves there, not here.
    // Look up a move by name: MoveRegistry.Get("Ember") → MoveData
    public static class MoveRegistry
    {
        private static readonly Dictionary<string, MoveData> _all = new();

        public static MoveData Get(string name) => _all[name];
        public static IEnumerable<MoveData> All => _all.Values;

        // Call once at startup before any moves are needed.
        // jsonPath: absolute path to moves.json
        public static void Load(string jsonPath)
        {
            _all.Clear();

            string json = File.ReadAllText(jsonPath);
            using JsonDocument doc = JsonDocument.Parse(json);

            foreach (JsonElement el in doc.RootElement.EnumerateArray())
            {
                string name        = el.GetProperty("name").GetString();
                string typeStr     = el.GetProperty("type").GetString();
                int    power       = el.GetProperty("power").GetInt32();
                int    accuracy    = el.GetProperty("accuracy").GetInt32();
                string description = el.GetProperty("description").GetString();

                var type = Enum.Parse<CreatureType>(typeStr);
                _all[name] = new MoveData(name, type, power, accuracy, description);
            }
        }
    }
}
