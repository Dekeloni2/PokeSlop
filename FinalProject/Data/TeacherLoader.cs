using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework;
using FinalProject.Battle;

namespace FinalProject.Data
{
    // loads a teacher from a JSON file. Bullet patterns are code so the JSON
    // only stores the pattern name, PatternRegistry resolves it.
    public static class TeacherLoader
    {
        public static TeacherStats Load(string jsonPath)
        {
            string json = File.ReadAllText(jsonPath);
            // comments aren't legal JSON, but these files are authored by hand
            // and get edited constantly — being able to // out a move to test
            // one attack in isolation, or leave a note next to a number, is
            // worth more here than staying strict.
            //
            // AllowTrailingCommas goes with it: commenting out the LAST entry
            // in a list always leaves the comma on the one before it, and
            // hunting that down every time is exactly the friction this is
            // meant to remove
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling         = JsonCommentHandling.Skip,
                AllowTrailingCommas         = true,
            };

            TeacherJson data = JsonSerializer.Deserialize<TeacherJson>(json, options)
                ?? throw new Exception($"Empty or invalid teacher JSON: {jsonPath}");

            var moves = new List<MoveData>();
            foreach (MoveJson m in data.Moves ?? new List<MoveJson>())
                moves.Add(new MoveData(m.Name, m.Power, m.Accuracy, m.Description,
                    PatternRegistry.Resolve(m.Pattern), m.IsUltimate, ToSpeech(m.Speech))
                {
                    Intro     = m.Intro,
                    IntroLeap = m.IntroLeap,
                });

            List<ActOption> actOptions = ToActOptions(data.ActOptions);

            List<PercentThresholdText> turnNarration = data.TurnNarrationByHp != null && data.TurnNarrationByHp.Count > 0
                ? ToThresholds(data.TurnNarrationByHp)
                : null;

            List<PercentThresholdText> spareTextByPercent = data.SpareTextByPercent != null && data.SpareTextByPercent.Count > 0
                ? ToThresholds(data.SpareTextByPercent)
                : null;

            List<PercentThresholdText> turnNarrationBySpare = data.TurnNarrationBySpare != null && data.TurnNarrationBySpare.Count > 0
                ? ToThresholds(data.TurnNarrationBySpare)
                : null;

            var stats = new TeacherStats(data.Name, data.SpriteName, data.SpeakingVoice, data.BaseHp, data.BaseAtk, moves, actOptions,
                turnNarration, data.SpareSuccessAt, data.SpareSuccessText, spareTextByPercent, turnNarrationBySpare,
                ToSprite(data.Sprite), ToDialogue(data.BattleDialogue), data.GoldReward, data.Theme, data.ThemeVolume, data.SequentialMoves);

            // a file without an explicit id falls back to the display name, so
            // older teacher files keep working untouched
            stats.Id         = string.IsNullOrWhiteSpace(data.Id) ? data.Name : data.Id;
            stats.GatedMoves   = data.GatedMoves;
            stats.Requires     = data.Requires ?? new List<string>();
            stats.LockedText   = data.LockedText;
            stats.UltimateAtHp = data.UltimateAtHp;
            stats.Yield        = data.Yield == null
                ? null
                : new TeacherYield(data.Yield.Speech, data.Yield.Closing, data.Yield.FlawlessLine,
                                   ToActOptions(data.Yield.ActOptions));

            return stats;
        }

        // a move's "speech" block: beat name -> the lines for it. empty beats are
        // dropped here rather than at lookup time, so a half filled block behaves
        // the same as one that was never written and the pattern falls back
        private static IReadOnlyDictionary<string, IReadOnlyList<string>> ToSpeech(
            Dictionary<string, List<string>> json)
        {
            if (json == null || json.Count == 0) return null;

            // case insensitive keys, so the file can say "Opening" or "opening"
            // and still match the beat name the pattern asks for
            var result = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (KeyValuePair<string, List<string>> beat in json)
            {
                if (beat.Value == null || beat.Value.Count == 0) continue;
                result[beat.Key] = beat.Value;
            }

            return result.Count > 0 ? result : null;
        }

        private static TeacherDialogue ToDialogue(DialogueJson json)
        {
            if (json == null) return null;

            return new TeacherDialogue(
                json.OnAttack, json.OnAct, json.OnItem, json.OnSpare,
                json.ByHp != null && json.ByHp.Count > 0 ? ToThresholds(json.ByHp) : null,
                json.OnDefeat, json.OnSpared);
        }

        // builds the multi part sprite from the "sprite" block. returns null if
        // the teacher doesn't have one so it just draws nothing
        private static TeacherSpriteData ToSprite(SpriteJson json)
        {
            if (json == null || json.Parts == null || json.Parts.Count == 0) return null;

            var parts = new List<TeacherPartData>();
            foreach (PartJson p in json.Parts)
            {
                if (p.Src == null || p.Src.Length < 4) continue; // bad entry, skip it

                var src = new Rectangle(p.Src[0], p.Src[1], p.Src[2], p.Src[3]);
                var offset = p.Offset != null && p.Offset.Length >= 2
                    ? new Vector2(p.Offset[0], p.Offset[1])
                    : Vector2.Zero;

                Rectangle? srcHurt = p.SrcHurt != null && p.SrcHurt.Length >= 4
                    ? new Rectangle(p.SrcHurt[0], p.SrcHurt[1], p.SrcHurt[2], p.SrcHurt[3])
                    : (Rectangle?)null;

                parts.Add(new TeacherPartData(p.Name, src, offset, p.BobX, p.BobY, p.Speed, p.Phase, srcHurt));
            }

            var anchor = json.Anchor != null && json.Anchor.Length >= 2
                ? new Vector2(json.Anchor[0], json.Anchor[1])
                : Vector2.Zero;

            return new TeacherSpriteData(json.Sheet, anchor, json.Scale, parts);
        }

        private static List<PercentThresholdText> ToThresholds(List<PercentThresholdTextJson> entries)
        {
            var result = new List<PercentThresholdText>();
            foreach (PercentThresholdTextJson e in entries)
                result.Add(new PercentThresholdText(e.MinPercent, e.Text));
            return result;
        }

        // raw shapes matching the JSON file
        private class TeacherJson
        {
            public string Name { get; set; }
            public string Id { get; set; }
            public List<string> Requires { get; set; }
            public string LockedText { get; set; }
            public int? UltimateAtHp { get; set; }
            public YieldJson Yield { get; set; }
            public string SpriteName { get; set; }
            public string SpeakingVoice { get; set; }
            public int BaseHp { get; set; }
            public int BaseAtk { get; set; }
            public List<MoveJson> Moves { get; set; }
            public List<ActOptionJson> ActOptions { get; set; }
            public List<PercentThresholdTextJson> TurnNarrationByHp { get; set; }
            public int SpareSuccessAt { get; set; } = 100;
            public string SpareSuccessText { get; set; }
            public List<PercentThresholdTextJson> SpareTextByPercent { get; set; }
            public List<PercentThresholdTextJson> TurnNarrationBySpare { get; set; }
            public SpriteJson Sprite { get; set; }
            public DialogueJson BattleDialogue { get; set; }
            public int GoldReward { get; set; }
            public string Theme { get; set; }
            public float ThemeVolume { get; set; } = 1f;
            public bool SequentialMoves { get; set; }
            public bool GatedMoves { get; set; }
        }

        private class DialogueJson
        {
            public List<string> OnAttack { get; set; }
            public List<string> OnAct { get; set; }
            public List<string> OnItem { get; set; }
            public List<string> OnSpare { get; set; }
            public List<PercentThresholdTextJson> ByHp { get; set; }
            public string OnDefeat { get; set; }
            public string OnSpared { get; set; }
        }

        private class SpriteJson
        {
            public string Sheet { get; set; }
            public float[] Anchor { get; set; }   // x,y nudge from the default spot
            public float Scale { get; set; } = 1f;
            public List<PartJson> Parts { get; set; }
        }

        private class PartJson
        {
            public string Name { get; set; }
            public int[] Src { get; set; }        // x,y,w,h on the sheet
            public int[] SrcHurt { get; set; }    // optional, used while hurt
            public float[] Offset { get; set; }   // x,y from the anchor
            public float BobX { get; set; }
            public float BobY { get; set; }
            public float Speed { get; set; } = 1f;
            public float Phase { get; set; }
        }

        private class MoveJson
        {
            public string Name { get; set; }
            public int Power { get; set; }
            public int Accuracy { get; set; }
            public string Description { get; set; }
            public string Pattern { get; set; }
            public bool IsUltimate { get; set; }

            // optional. a cutscene before the attack, see MoveData.Intro
            public string Intro { get; set; }

            // optional. he leaps off screen once the intro is read, see
            // MoveData.IntroLeap
            public bool IntroLeap { get; set; }

            // optional. beat name -> lines, see MoveData.Line
            public Dictionary<string, List<string>> Speech { get; set; }
        }

        private class YieldJson
        {
            public string Speech { get; set; }
            public string Closing { get; set; }
            public string FlawlessLine { get; set; }

            // optional. replaces his ACT list once he's yielded
            public List<ActOptionJson> ActOptions { get; set; }
        }

        // shared by the teacher's normal ACT list and the shorter one he's left
        // with after yielding, so both support the same fields
        private static List<ActOption> ToActOptions(List<ActOptionJson> json)
        {
            var options = new List<ActOption>();

            foreach (ActOptionJson a in json ?? new List<ActOptionJson>())
            {
                ActOption option = a.DescriptionByHp != null && a.DescriptionByHp.Count > 0
                    ? new ActOption(a.Name, ToThresholds(a.DescriptionByHp), a.SpareGain, a.ExtraMessages, a.ForcesPattern)
                    : new ActOption(a.Name, a.Description, a.SpareGain, a.ExtraMessages, a.ForcesPattern);

                option.Speech = a.Speech;
                options.Add(option);
            }

            return options;
        }

        private class ActOptionJson
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public List<PercentThresholdTextJson> DescriptionByHp { get; set; }
            public int SpareGain { get; set; }
            public List<string> ExtraMessages { get; set; }
            public string ForcesPattern { get; set; }
            public string Speech { get; set; }
        }

        private class PercentThresholdTextJson
        {
            public int MinPercent { get; set; }
            public string Text { get; set; }
        }
    }
}
