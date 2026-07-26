using System;
using System.Collections.Generic;
using FinalProject.Battle;

namespace FinalProject.Data
{
    // data for one enemy move
    public class MoveData
    {
        public string      Name        { get; }
        public int         Power       { get; }    // 0 = status move, no damage
        public int         Accuracy    { get; }    // 0-100
        public string      Description { get; }

        // factory for this move's bullet pattern. Each turn gets a fresh
        // instance since patterns keep their own spawn timers
        public Func<IBulletPattern> CreatePattern { get; }

        public bool IsStatusMove => Power == 0;

        // a desperation move. BattleState saves it for the turn the fight is
        // about to end (see PickEnemyMove) instead of putting it in the rotation
        public bool IsUltimate { get; }

        // lines the teacher says during this attack, keyed by a beat name the
        // pattern picks ("opening", "cleared", ...). Quiz prompts stay in code
        // because the mechanic depends on their wording — this is for the text
        // that only carries character, so it can be rewritten without a rebuild
        public IReadOnlyDictionary<string, IReadOnlyList<string>> Speech { get; }

        public MoveData(string name, int power, int accuracy, string description,
            Func<IBulletPattern> createPattern, bool isUltimate = false,
            IReadOnlyDictionary<string, IReadOnlyList<string>> speech = null)
        {
            Name          = name;
            Power         = power;
            Accuracy      = accuracy;
            Description   = description;
            CreatePattern = createPattern;
            IsUltimate    = isUltimate;
            Speech        = speech;
        }

        // one authored line for a beat, or null if the JSON doesn't cover it —
        // callers pass their own fallback so a half written block degrades to
        // the pattern's built in script instead of an empty bubble.
        //
        // variant picks between several lines for the same beat. the chess board
        // passes its board number, so each escalation gets its own script.
        // running off the end repeats the last entry, which means authoring
        // three lines for a beat is enough to cover every board after that
        public string Line(string beat, int variant)
        {
            if (Speech == null || string.IsNullOrEmpty(beat))            return null;
            if (!Speech.TryGetValue(beat, out IReadOnlyList<string> ls)) return null;
            if (ls == null || ls.Count == 0)                             return null;

            return ls[Math.Clamp(variant, 0, ls.Count - 1)];
        }
    }
}
