using System;

namespace FinalProject.World
{
    // what an INpcInteraction is allowed to touch when it triggers — mirrors
    // DodgeContext for bullet patterns. StartBattleById is a callback rather
    // than a direct method because actually pushing a battle needs the
    // camera and player position, which only OverworldState has; this keeps
    // World from depending back on States.
    public class NpcInteractionContext
    {
        private readonly Action<string> _startBattleById;

        public NpcInteractionContext(Action<string> startBattleById)
        {
            _startBattleById = startBattleById;
        }

        public void StartBattleById(string id) => _startBattleById(id);
    }
}
