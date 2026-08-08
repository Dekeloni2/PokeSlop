namespace FinalProject.World
{
    // the default NPC behavior, and the only one that exists today: bumping
    // it starts a fight against the Teacher whose id matches the NPC's own.
    // every NPC on every map uses this unless its Kind says otherwise.
    public class BattleInteraction : INpcInteraction
    {
        public void Trigger(Npc npc, NpcInteractionContext context)
            => context.StartBattleById(npc.Id);
    }
}
