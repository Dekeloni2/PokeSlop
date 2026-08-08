using System;
using System.Collections.Generic;

namespace FinalProject.World
{
    // maps an Npc's "Kind" property to the interaction it runs on bump.
    // Mirrors PatternRegistry for bullet patterns — a new interaction needs
    // an entry here to be usable from a map's NPC object.
    public static class NpcInteractionRegistry
    {
        // every NPC placed before Kind existed has no such property, so this
        // is what an unset Kind resolves to — keeps old maps working as-is
        public const string DefaultKind = "Battle";

        private static readonly Dictionary<string, Func<INpcInteraction>> Interactions = new()
        {
            [DefaultKind] = () => new BattleInteraction(),
        };

        public static INpcInteraction Resolve(string kind)
        {
            if (Interactions.TryGetValue(kind, out Func<INpcInteraction> factory))
                return factory();

            throw new ArgumentException($"Unknown NPC interaction \"{kind}\", add it to NpcInteractionRegistry.");
        }
    }
}
