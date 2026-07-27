using FinalProject;
using FinalProject.Core;

namespace FinalProject.Events

{
    public class AreaChangedEvent : GameEvent
    {
        
        // Fired when the player moves into a new area.
        // Listeners: HUD, AudioManager
        public string NewAreaName { get;  }
        public AreaChangedEvent(string newAreaName)
        {
            NewAreaName = newAreaName;
        }
    }

    // how a teacher's fight finished. only the two ways a teacher actually gets
    // resolved — losing to him doesn't settle anything, you just try again
    public enum BattleOutcome { Killed, Spared }

    // Fired once a teacher's fight is decided, before the ending plays out.
    // The teacher is a name rather than a type so a new teacher stays a JSON
    // file and nothing here has to change.
    // Listeners: RouteTracker
    public class TeacherResolvedEvent : GameEvent
    {
        public string        TeacherName { get; }
        public BattleOutcome Outcome     { get; }

        public TeacherResolvedEvent(string teacherName, BattleOutcome outcome)
        {
            TeacherName = teacherName;
            Outcome     = outcome;
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