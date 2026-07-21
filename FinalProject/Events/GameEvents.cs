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

    // Fired the moment a battle begins.
    // Listners: OverworldState (stop processing the movement/encounters)
    public class BattleStartedEvent : GameEvent
    {
        public bool IsTeacherBattle { get;  }

        public BattleStartedEvent(bool isTeacherBattle)
        {
            IsTeacherBattle = isTeacherBattle;
        }
    }

    
    // Fired when the battle ends.
    // Listners: OverworldState (re-enabled movement)
    public class BattleEndedEvent : GameEvent
    {
        public bool PlayerWon { get;  }

        public BattleEndedEvent(bool playerWon)
        {
            PlayerWon = playerWon;
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

    
    // Fired when the player reaches a teacher's classroom. Triggers the end
    // game sequence.
    public class ClassReachedEvent : GameEvent
    {
        public string ClassName { get; }

        public ClassReachedEvent(string className)
        {
            ClassName = className;
        }
    }
}