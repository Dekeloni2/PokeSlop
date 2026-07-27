using System.Collections.Generic;
using System.Text;

namespace FinalProject.Data
{
    // a teacher's last stand. once his ultimate is survived he stops fighting,
    // says his piece with the action buttons off screen, and hands the decision
    // to the player — mercy or not, but he won't defend himself either way.
    //
    // the speech is split in two so a line about the player's performance can
    // land in the middle of it rather than tacked on the end
    public class TeacherYield
    {
        // '|' between pages, same as everywhere else text is authored
        public string Speech       { get; }
        public string Closing      { get; }

        // only shown when the player got through the whole fight untouched
        public string FlawlessLine { get; }

        // what ACT offers once he's yielded, replacing his usual list — there's
        // nothing left to talk him round with. empty keeps the normal options
        public IReadOnlyList<ActOption> ActOptions { get; }

        public TeacherYield(string speech, string closing, string flawlessLine,
            IReadOnlyList<ActOption> actOptions = null)
        {
            Speech       = speech;
            Closing      = closing;
            FlawlessLine = flawlessLine;
            ActOptions   = actOptions ?? new List<ActOption>();
        }

        // the whole speech as one page-broken string, with the flawless line
        // folded in only if it was earned
        public string BuildSpeech(bool flawless)
        {
            var text = new StringBuilder(Speech);

            if (flawless && !string.IsNullOrWhiteSpace(FlawlessLine))
                Append(text, FlawlessLine);

            Append(text, Closing);
            return text.ToString();
        }

        private static void Append(StringBuilder text, string part)
        {
            if (string.IsNullOrWhiteSpace(part)) return;

            if (text.Length > 0) text.Append('|');
            text.Append(part);
        }
    }
}
