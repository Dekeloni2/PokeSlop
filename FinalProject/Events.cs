using FinalProject;

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

    
    // Fired when RNG decides a wild encouter triggers in tall grass.
    // Listeners: OverworldState (push BattleState)
    public class WildEncouterTriggeredEvent : GameEvent
    {
        public string ZoneName { get;  }

        public WildEncouterTriggeredEvent(string zoneName)
        {
            ZoneName = zoneName;
        }
    }

    // Fired the moment a battle begins.
    // Listners: OverworldState (stop processing the movement/encounters)
    public class BattleStartedEvent : GameEvent
    {
        public bool IsTrainerBattle { get;  }

        public BattleStartedEvent(bool isTrainerBattle)
        {
            IsTrainerBattle = isTrainerBattle;
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

    
    // Fired when the player's creature HP changes.
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

    
    // Fired when the player reaches the gym. Triggers end game sequence.
    public class GymReachedEvent : GameEvent
    {
        public string GymName { get; }

        public GymReachedEvent(string gymName)
        {
            GymName = gymName;
        }
    }
}