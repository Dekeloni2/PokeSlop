namespace FinalProject.Data
{
    public enum ReactionEffect
    {
        None,
        Vapor,       // accuracy down on hit
        Crystalize,  // normal type attacks deal more damage to affected creature
        Inferno,     // burn like pokemon
        Explosion,   // AOE damage to all other creatures on the field
        Mud,         // speed down on hit
        Frost,       // frozen like pokemon
        Conduction,  // disables the last used move, takes damage every turn
        Sandstorm,   // sandstorm on field for 3 turns
        Magnetize,   // can't switch out or run away
        Lightning    // paralyze like pokemon
    }
}
