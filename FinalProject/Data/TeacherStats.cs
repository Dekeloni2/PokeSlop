using System.Collections.Generic;

namespace FinalProject.Data
{
    // the fixed data for one boss teacher, loaded from JSON
    public class TeacherStats
    {
        public string      Name          { get; }

        // stable identifier for save data and unlock checks, so renaming what's
        // printed on screen can't silently break a gate. the loader falls back
        // to Name when a file doesn't set one
        public string Id { get; set; }

        // teachers (by Id) whose fights have to be finished before this one can
        // start. empty means he's available from the off
        public IReadOnlyList<string> Requires { get; set; } = new List<string>();

        // shown at his door while Requires isn't met
        public string LockedText { get; set; }

        // HP at or below which he reaches for his ultimate. null means the
        // default, "one more clean hit would finish him", which moves about
        // with how hard the player is swinging — set this to pin it
        public int? UltimateAtHp { get; set; }

        // his last stand once the ultimate has been survived. null means he
        // just keeps fighting like anyone else
        public TeacherYield Yield { get; set; }

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

        // what he says in the speech bubble, keyed by what the player did
        public TeacherDialogue Dialogue { get; }

        // handed to the player when the fight is won, either way it ended
        public int GoldReward { get; }

        // name of an item from items.json dropped alongside the gold, for the
        // teachers who hand over a piece of equipment. null means gold only
        public string ItemReward { get; set; }

        // the final fight. winning it rolls into the ending instead of dropping
        // back to the overworld, so which fight is last stays a JSON decision
        public bool EndsGame { get; set; }

        // SoundManager key for his battle music, null means the fight is silent.
        // ThemeVolume is 0..1 for balancing it against everything else
        public string Theme { get; }
        public float  ThemeVolume { get; }

        // moves play in JSON order instead of at random. for teachers whose
        // attacks are lessons that build on each other, like Yakir
        public bool SequentialMoves { get; }

        // with SequentialMoves, only step to the next move when the player got
        // through the last one untouched — so a lesson repeats until it lands.
        // clearing the final move is also what hands over mercy. for the
        // training dummy, where the attacks are the tutorial
        public bool GatedMoves { get; set; }

        // SoundManager key for his text blip while speaking. null means the
        // default "beep" is used.
        public string SpeakingVoice { get; }

        public TeacherStats(
            string name, string spriteName, string speakingVoice, int baseHp, int baseAtk,
            IReadOnlyList<MoveData> moves, IReadOnlyList<ActOption> actOptions = null,
            IReadOnlyList<PercentThresholdText> turnNarration = null,
            int spareSuccessAt = 100, string spareSuccessText = null,
            IReadOnlyList<PercentThresholdText> spareTextByPercent = null,
            IReadOnlyList<PercentThresholdText> turnNarrationBySpare = null,
            TeacherSpriteData sprite = null,
            TeacherDialogue dialogue = null, int goldReward = 0,
            string theme = null, float themeVolume = 1f, bool sequentialMoves = false)
        {
            Sprite      = sprite;
            Dialogue    = dialogue;
            GoldReward  = goldReward;
            Theme       = theme;
            ThemeVolume = themeVolume <= 0f ? 1f : themeVolume;
            SequentialMoves = sequentialMoves;
            SpeakingVoice = speakingVoice;
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
