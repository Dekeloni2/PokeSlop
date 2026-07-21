using System.Collections.Generic;

namespace FinalProject.Data
{
    // what a teacher says in the speech bubble, split by what the player did.
    // each list steps on its own, so attacking twice gives OnAttack[0] then
    // OnAttack[1] no matter how many times you ACTed in between.
    // ByHp overrides all of them when his health matches an entry
    public class TeacherDialogue
    {
        public IReadOnlyList<string> OnAttack { get; }
        public IReadOnlyList<string> OnAct    { get; }
        public IReadOnlyList<string> OnItem   { get; }
        public IReadOnlyList<string> OnSpare  { get; }

        public IReadOnlyList<PercentThresholdText> ByHp { get; }

        // his last words. OnDefeat plays before he turns to dust, OnSpared
        // before he freezes. both support '|' for multiple bubbles
        public string OnDefeat { get; }
        public string OnSpared { get; }

        public TeacherDialogue(
            IReadOnlyList<string> onAttack, IReadOnlyList<string> onAct,
            IReadOnlyList<string> onItem,   IReadOnlyList<string> onSpare,
            IReadOnlyList<PercentThresholdText> byHp,
            string onDefeat = null, string onSpared = null)
        {
            OnAttack = onAttack;
            OnAct    = onAct;
            OnItem   = onItem;
            OnSpare  = onSpare;
            ByHp     = byHp;
            OnDefeat = onDefeat;
            OnSpared = onSpared;
        }
    }
}
