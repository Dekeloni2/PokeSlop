using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Xna.Framework;

namespace FinalProject.Data
{
    // One spoken line of an ending's phone call. Speaker is a teacher id, as
    // named by his JSON file — that's where the face and the voice come from.
    // Leave Speaker out for narration: no portrait, default blip.
    public class EndingLine
    {
        public string Speaker { get; set; }
        public string Text    { get; set; } = "";
    }

    // One ending. Which one plays is decided by Killed: the exact set of
    // teachers the player killed this run. An empty list is the pacifist run,
    // all three is genocide, and everything between is its own script.
    //
    // Lines are the phone call, shown in the dialogue box with portraits.
    // Pages are the closing narration afterwards, centred under the title.
    // Either may be empty — genocide is mostly narration, pacifist mostly call.
    public class EndingText
    {
        public string Id { get; set; } = "";

        public List<string> Killed { get; set; } = new();

        public string Title { get; set; } = "THE END";

        // [r, g, b], 0-255. Missing or short means plain white.
        public List<int> TitleColor { get; set; }

        // SoundManager song key, or null for silence
        public string Music { get; set; }

        public List<EndingLine> Lines { get; set; } = new();
        public List<string>     Pages { get; set; } = new();

        public Color ResolvedTitleColor => TitleColor != null && TitleColor.Count >= 3
            ? new Color(TitleColor[0], TitleColor[1], TitleColor[2])
            : Color.White;
    }

    // Loaded from Content/endings.json. Add or reword an ending in the JSON —
    // no code changes needed.
    public class EndingConfig
    {
        public List<EndingText> Endings { get; set; } = new();

        public static EndingConfig Load(string path)
        {
            if (!File.Exists(path)) return new EndingConfig();

            try
            {
                string json = File.ReadAllText(path);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return JsonSerializer.Deserialize<EndingConfig>(json, options) ?? new EndingConfig();
            }
            catch { return new EndingConfig(); }
        }

        // The ending whose Killed list matches exactly who died this run —
        // order and casing don't matter. Falls back to something playable rather
        // than a blank screen if the run doesn't match any authored combination
        // (a half-finished debug run, or a teacher added without a new ending).
        public EndingText For(IEnumerable<string> killedIds)
        {
            var killed = new HashSet<string>(killedIds ?? Enumerable.Empty<string>(),
                                             StringComparer.OrdinalIgnoreCase);

            foreach (EndingText ending in Endings)
            {
                var authored = new HashSet<string>(ending.Killed ?? new List<string>(),
                                                   StringComparer.OrdinalIgnoreCase);
                if (authored.SetEquals(killed)) return ending;
            }

            return Fallback(killed.Count);
        }

        private static EndingText Fallback(int killedCount) => new EndingText
        {
            Id    = "fallback",
            Title = "THE END",
            Pages = { killedCount == 0
                ? "You left Tiltan without hurting anyone."
                : "You left Tiltan. Not everyone did." }
        };
    }
}
