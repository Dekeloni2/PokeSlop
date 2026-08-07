using FinalProject;
using FinalProject.Core;

namespace FinalProject.Events

{
    // how a teacher's fight finished. only the two ways a teacher actually gets
    // resolved — losing to him doesn't settle anything, you just try again
    public enum BattleOutcome { Killed, Spared }

    // Fired once a teacher's fight is decided, before the ending plays out.
    // The teacher is an id rather than a type so a new teacher stays a JSON
    // file and nothing here has to change — and an id rather than a display
    // name so renaming him on screen can't break an unlock that depends on him.
    // Listeners: RouteTracker
    public class TeacherResolvedEvent : GameEvent
    {
        public string        TeacherId { get; }
        public BattleOutcome Outcome   { get; }

        public TeacherResolvedEvent(string teacherId, BattleOutcome outcome)
        {
            TeacherId = teacherId;
            Outcome   = outcome;
        }
    }


    // Fired when the player's HP changes.
    // Listeners: BattleHUD
    public class PlayerHpChangedEvent : GameEvent
    {
        public int CurrentHp { get;  }
        public int MaxHp { get;  }
        public PlayerHpChangedEvent(int currentHp, int maxHp)
        {
            CurrentHp = currentHp;
            MaxHp = maxHp;
        }
    }
}