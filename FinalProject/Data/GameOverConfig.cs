using System.Collections.Generic;
using System.Text.Json;
using FinalProject.Core;

namespace FinalProject.Data
{
    // Loaded from Content/gameover.json. The game-over screen picks one scenario
    // at random; each scenario uses '|' to split into pages, like the teacher
    // dialogue. Add/edit scenarios in the JSON — no code changes needed.
    public class GameOverConfig
    {
        public string Title { get; set; } = "GAME OVER";
        public List<string> Scenarios { get; set; } = new();

        public static GameOverConfig Load(string path)
        {
            // one read rather than an exists-then-read, which on the web would
            // mean fetching the file twice — see ContentFiles.Exists
            string json = ContentFiles.ReadAllTextOrNull(path);
            if (json == null) return Fallback();

            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                GameOverConfig cfg = JsonSerializer.Deserialize<GameOverConfig>(json, options);

                if (cfg == null || cfg.Scenarios == null || cfg.Scenarios.Count == 0)
                    return Fallback();
                return cfg;
            }
            catch { return Fallback(); }
        }

        private static GameOverConfig Fallback() => new GameOverConfig
        {
            Title = "GAME OVER",
            Scenarios = { "Stay determined." }
        };
    }
}
