using Microsoft.Xna.Framework;

namespace FinalProject.Battle
{
    // the undertale style rule a hazard plays by. White always hurts; Blue only
    // catches you moving; Orange only catches you standing still.
    //
    // this is the one mechanic the game shares across teachers — the training
    // dummy teaches it, Dor's lash uses it, David's punch and the hexagons in
    // his ultimate quote it back. it lives here because it was previously four
    // separate copies (one per pattern), each with its own enum, its own pair of
    // colours and its own moving/still test. the colour IS the tell, so a copy
    // drifting by a few RGB points or getting the test backwards would quietly
    // stop it being one rule the player can learn once.
    public enum HazardRule { White, Blue, Orange }

    public static class HazardRules
    {
        public static readonly Color BlueColor   = new Color(60, 130, 255);
        public static readonly Color OrangeColor = new Color(255, 150, 40);

        // what the hazard is drawn in. White hazards keep the plain colour they
        // always had, so nothing that predates the rule changes appearance
        public static Color Tint(this HazardRule rule) => rule switch
        {
            HazardRule.Blue   => BlueColor,
            HazardRule.Orange => OrangeColor,
            _                 => Color.White,
        };

        // whether this hazard actually lands, given what the player is doing
        public static bool Connects(this HazardRule rule, bool moving) => rule switch
        {
            HazardRule.Blue   => moving,
            HazardRule.Orange => !moving,
            _                 => true,
        };
    }
}
