using System.Collections.Generic;

namespace FinalProject.Data
{
    // one entry in a teacher's ACT menu. The description is either fixed or
    // changes with the teacher's HP, extraMessages are optional follow-up
    // lines, and SpareGain is how much mercy progress picking it gives.
    // ForcesPattern lets an ACT provoke a specific attack out of the teacher
    // instead of whatever he had lined up, for the ones that get under his skin
    public class ActOption
    {
        public string Name          { get; }
        public int    SpareGain     { get; }
        public string ForcesPattern { get; }

        // what the teacher says back, in his speech bubble rather than the box.
        // overrides his usual onAct line for this turn, '|' splits it into pages
        public string Speech { get; set; }

        private readonly string _description;
        private readonly IReadOnlyList<PercentThresholdText> _descriptionByHp;
        private readonly IReadOnlyList<string> _extraMessages;

        public ActOption(string name, string description, int spareGain = 0,
            IReadOnlyList<string> extraMessages = null, string forcesPattern = null)
        {
            Name           = name;
            _description   = description;
            SpareGain      = spareGain;
            _extraMessages = extraMessages;
            ForcesPattern  = forcesPattern;
        }

        public ActOption(string name, IReadOnlyList<PercentThresholdText> descriptionByHp, int spareGain = 0,
            IReadOnlyList<string> extraMessages = null, string forcesPattern = null)
        {
            Name             = name;
            _descriptionByHp = descriptionByHp;
            SpareGain        = spareGain;
            _extraMessages   = extraMessages;
            ForcesPattern    = forcesPattern;
        }

        public string GetDescription(float hpPercent)
            => _descriptionByHp != null ? PercentThresholdText.Resolve(_descriptionByHp, hpPercent) : _description;

        // description first, then any follow-up lines
        public List<string> GetMessages(float hpPercent)
        {
            var messages = new List<string> { GetDescription(hpPercent) };
            if (_extraMessages != null)
                messages.AddRange(_extraMessages);
            return messages;
        }
    }
}
