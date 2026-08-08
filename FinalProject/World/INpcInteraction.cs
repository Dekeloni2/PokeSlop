namespace FinalProject.World
{
    // what happens when the player bumps into an Npc. Mirrors IBulletPattern:
    // NPCs vary by behavior through implementations of this interface,
    // resolved by Kind through NpcInteractionRegistry, rather than by
    // subclassing Npc itself.
    public interface INpcInteraction
    {
        void Trigger(Npc npc, NpcInteractionContext context);
    }
}
