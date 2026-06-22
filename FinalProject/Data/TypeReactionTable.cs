namespace FinalProject.Data
{
    // Maps two types to the reaction they produce when combined.
    // Usage: TypeReactionTable.GetReaction(CreatureType.Fire, CreatureType.Water) → Vapor
    public static class TypeReactionTable
    {
        // Indexed by (int)attacker type, (int)defender type
        // Matches enum order: Normal=0, Fire=1, Water=2, Earth=3, Wind=4, Electricity=5
        private static readonly ReactionEffect[,] _table =
        {
            //               Normal               Fire                  Water                 Earth                 Wind                  Electricity
            /* Normal */  {  ReactionEffect.None,  ReactionEffect.None,  ReactionEffect.None,  ReactionEffect.None,  ReactionEffect.None,  ReactionEffect.None       },
            /* Fire    */  {  ReactionEffect.None,  ReactionEffect.None,  ReactionEffect.Vapor, ReactionEffect.Crystalize, ReactionEffect.Inferno, ReactionEffect.Explosion  },
            /* Water   */  {  ReactionEffect.None,  ReactionEffect.Vapor, ReactionEffect.None,  ReactionEffect.Mud,   ReactionEffect.Frost,  ReactionEffect.Conduction },
            /* Earth   */  {  ReactionEffect.None,  ReactionEffect.Crystalize, ReactionEffect.Mud, ReactionEffect.None, ReactionEffect.Sandstorm, ReactionEffect.Magnetize },
            /* Wind    */  {  ReactionEffect.None,  ReactionEffect.Inferno, ReactionEffect.Frost, ReactionEffect.Sandstorm, ReactionEffect.None, ReactionEffect.Lightning },
            /* Elec    */  {  ReactionEffect.None,  ReactionEffect.Explosion, ReactionEffect.Conduction, ReactionEffect.Magnetize, ReactionEffect.Lightning, ReactionEffect.None },
        };

        public static ReactionEffect GetReaction(CreatureType attacker, CreatureType defender)
            => _table[(int)attacker, (int)defender];
    }
}
