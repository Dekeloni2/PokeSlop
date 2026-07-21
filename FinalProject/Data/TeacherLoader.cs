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
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            TeacherJson data = JsonSerializer.Deserialize<TeacherJson>(json, options)
                ?? throw new Exception($"Empty or invalid teacher JSON: {jsonPath}");

            var moves = new List<MoveData>();
            foreach (MoveJson m in data.Moves ?? new List<MoveJson>())
                moves.Add(new MoveData(m.Name, m.Power, m.Accuracy, m.Description,
                    PatternRegistry.Resolve(m.Pattern)));

            var actOptions = new List<ActOption>();
            foreach (ActOptionJson a in data.ActOptions ?? new List<ActOptionJson>())
            {
                if (a.DescriptionByHp != null && a.DescriptionByHp.Count > 0)
                    actOptions.Add(new ActOption(a.Name, ToThresholds(a.DescriptionByHp), a.SpareGain, a.ExtraMessages));
                else
                    actOptions.Add(new ActOption(a.Name, a.Description, a.SpareGain, a.ExtraMessages));
            }

            List<PercentThresholdText> turnNarration = data.TurnNarrationByHp != null && data.TurnNarrationByHp.Count > 0
                ? ToThresholds(data.TurnNarrationByHp)
                : null;

            List<PercentThresholdText> spareTextByPercent = data.SpareTextByPercent != null && data.SpareTextByPercent.Count > 0
                ? ToThresholds(data.SpareTextByPercent)
                : null;

            List<PercentThresholdText> turnNarrationBySpare = data.TurnNarrationBySpare != null && data.TurnNarrationBySpare.Count > 0
                ? ToThresholds(data.TurnNarrationBySpare)
                : null;

            return new TeacherStats(data.Name, data.SpriteName, data.BaseHp, data.BaseAtk, moves, actOptions,
                turnNarration, data.SpareSuccessAt, data.SpareSuccessText, spareTextByPercent, turnNarrationBySpare,
                ToSprite(data.Sprite));
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
            public string SpriteName { get; set; }
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
        }

        private class ActOptionJson
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public List<PercentThresholdTextJson> DescriptionByHp { get; set; }
            public int SpareGain { get; set; }
            public List<string> ExtraMessages { get; set; }
        }

        private class PercentThresholdTextJson
        {
            public int MinPercent { get; set; }
            public string Text { get; set; }
        }
    }
}
