using System.Collections.Generic;

namespace FinalProject.Data
{
    // text tied to a percentage band. Used for HP-based descriptions and
    // narration, and for the spare% mercy texts.
    public class PercentThresholdText
    {
        public int    MinPercent { get; }
        public string Text       { get; }

        public PercentThresholdText(int minPercent, string text)
        {
            MinPercent = minPercent;
            Text       = text;
        }

        // picks the highest band that the given percent still falls into
        public static string Resolve(IReadOnlyList<PercentThresholdText> entries, float percent)
        {
            PercentThresholdText best = null;

            foreach (PercentThresholdText e in entries)
                if (percent >= e.MinPercent && (best == null || e.MinPercent > best.MinPercent))
                    best = e;

            return best?.Text ?? entries[entries.Count - 1].Text;
        }
    }
}
