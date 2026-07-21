using System.Collections.Generic;

namespace FinalProject.Data
{
    // the fixed data for one boss teacher, loaded from JSON
    public class TeacherStats
    {
        public string      Name          { get; }
        public string      SpriteName    { get; }
        public int         BaseHp        { get; }
        public int         BaseAtk       { get; }
        public IReadOnlyList<MoveData>  Moves       { get; }
        public IReadOnlyList<ActOption> ActOptions  { get; }

        // narration shown while picking an action, changes with the teacher's HP%
        public IReadOnlyList<PercentThresholdText> TurnNarration { get; }

        // spare% needed for mercy to actually work, the text shown when it
        // does, and the texts shown while it doesn't (by current spare%)
        public int    SpareSuccessAt   { get; }
        public string SpareSuccessText { get; }
        public IReadOnlyList<PercentThresholdText> SpareTextByPercent { get; }

        // optional narration keyed by spare%, shown instead of the HP narration
        // when the spare% has overtaken the HP% (see BattleState.RefreshNarration)
        public IReadOnlyList<PercentThresholdText> TurnNarrationBySpare { get; }

        // the multi part sprite, null if the teacher's JSON has no "sprite" block
        public TeacherSpriteData Sprite { get; }

        public TeacherStats(
            string name, string spriteName, int baseHp, int baseAtk,
            IReadOnlyList<MoveData> moves, IReadOnlyList<ActOption> actOptions = null,
            IReadOnlyList<PercentThresholdText> turnNarration = null,
            int spareSuccessAt = 100, string spareSuccessText = null,
            IReadOnlyList<PercentThresholdText> spareTextByPercent = null,
            IReadOnlyList<PercentThresholdText> turnNarrationBySpare = null,
            TeacherSpriteData sprite = null)
        {
            Sprite = sprite;
            Name       = name;
            SpriteName = spriteName;
            BaseHp     = baseHp;
            BaseAtk    = baseAtk;
            Moves      = moves ?? new List<MoveData>();
            ActOptions = actOptions ?? new List<ActOption>();
            TurnNarration = turnNarration ?? new List<PercentThresholdText>
            {
                new PercentThresholdText(0, $"{name} prepares to attack!")
            };

            SpareSuccessAt   = spareSuccessAt;
            SpareSuccessText = spareSuccessText ?? $"{name} accepts your mercy!";
            SpareTextByPercent = spareTextByPercent ?? new List<PercentThresholdText>
            {
                new PercentThresholdText(0, $"{name} doesn't seem ready to be spared yet.")
            };

            // optional; null means "always use the HP narration"
            TurnNarrationBySpare = turnNarrationBySpare;
        }
    }
}
