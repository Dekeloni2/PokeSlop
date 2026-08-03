using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Xna.Framework;

namespace FinalProject.Data
{
    // One entry in an ending's script. Speaker is a teacher id, as named by his
    // JSON file — that's where the face and the voice come from. Leave Speaker
    // out for narration: no portrait, default blip.
    //
    // An entry can also cue music, placed wherever it belongs in the script:
    //
    //   { "music": "tiltantale_ending" }   start the track here
    //   { "musicStop": true }              fade it out here
    //   { "musicCut": true }               stop it dead here, no fade
    //
    // An entry with no Text isn't spoken at all — it fires its cue and the
    // script moves straight on. Put Music on a line that does have text and the
    // track starts as that line comes up.
    public class EndingLine
    {
        public string Speaker { get; set; }
        public string Text    { get; set; } = "";

        // SoundManager song key
        public string Music     { get; set; }

        // fades the track out over MusicFadeSeconds
        public bool MusicStop { get; set; }

        // kills it on the spot, no fade — for a line that should land in sudden
        // silence rather than have the music ebb away under it
        public bool MusicCut { get; set; }

        public float MusicVolume      { get; set; } = 0.6f;
        public float MusicFadeSeconds { get; set; } = 2f;

        // Seconds to hold before the next dialogue box opens. Only meaningful on
        // a cue-only entry — a spoken line brings its own box up immediately.
        // Leave it out and a music cue gets a default beat so the track has room
        // to breathe before anyone talks over it; set 0 to cut straight in.
        public float? Delay { get; set; }

        public bool IsCueOnly => string.IsNullOrEmpty(Text);
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
            if (!File.Exists(path))
            {
                LogDebug($"endings.json not found at {path}");
                return new EndingConfig();
            }

            try
            {
                string json = File.ReadAllText(path);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    // this file is written by hand and runs long, so forgive the
                    // two things that trip authors up most
                    AllowTrailingCommas = true,
                    ReadCommentHandling = JsonCommentHandling.Skip
                };
                return JsonSerializer.Deserialize<EndingConfig>(json, options) ?? new EndingConfig();
            }
            catch (Exception e)
            {
                // A malformed file used to fail silently: every ending quietly
                // became the fallback, which reads as "the wrong ending played"
                // rather than "the JSON is broken". Say so instead.
                LogDebug($"ENDINGS PARSE ERROR ({path}): {e.Message}");
                return new EndingConfig();
            }
        }

        // next to the executable, same place MapLoader logs — there's no console
        // attached to look at
        private static void LogDebug(string message)
        {
            try
            {
                File.AppendAllText(
                    Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "map_debug.txt")),
                    message + "\n");
            }
            catch { }
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
