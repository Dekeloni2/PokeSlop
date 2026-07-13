using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
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
                turnNarration, data.SpareSuccessAt, data.SpareSuccessText, spareTextByPercent, turnNarrationBySpare);
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
