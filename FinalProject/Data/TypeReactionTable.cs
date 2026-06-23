using System.Collections.Generic;

namespace FinalProject.Data
{
    // Maps two types to the reaction they produce when combined.
    // Usage: TypeReactionTable.GetReaction(CreatureType.Fire, CreatureType.Water) → Vapor
    //
    // To add a new type: add a CreatureType enum value, then add entries here for every
    // (newType, existingType) and (existingType, newType) pair that should react.
    // Only non-None reactions need an entry — everything else defaults to None.
    public static class TypeReactionTable
    {
        private static readonly Dictionary<(CreatureType, CreatureType), ReactionEffect> _table = new()
        {
            { (CreatureType.Fire,        CreatureType.Water),       ReactionEffect.Vapor       },
            { (CreatureType.Water,       CreatureType.Fire),        ReactionEffect.Vapor       },
            { (CreatureType.Fire,        CreatureType.Earth),       ReactionEffect.Crystalize  },
            { (CreatureType.Earth,       CreatureType.Fire),        ReactionEffect.Crystalize  },
            { (CreatureType.Fire,        CreatureType.Wind),        ReactionEffect.Inferno     },
            { (CreatureType.Wind,        CreatureType.Fire),        ReactionEffect.Inferno     },
            { (CreatureType.Fire,        CreatureType.Electricity), ReactionEffect.Explosion   },
            { (CreatureType.Electricity, CreatureType.Fire),        ReactionEffect.Explosion   },
            { (CreatureType.Water,       CreatureType.Earth),       ReactionEffect.Mud         },
            { (CreatureType.Earth,       CreatureType.Water),       ReactionEffect.Mud         },
            { (CreatureType.Water,       CreatureType.Wind),        ReactionEffect.Frost       },
            { (CreatureType.Wind,        CreatureType.Water),       ReactionEffect.Frost       },
            { (CreatureType.Water,       CreatureType.Electricity), ReactionEffect.Conduction  },
            { (CreatureType.Electricity, CreatureType.Water),       ReactionEffect.Conduction  },
            { (CreatureType.Earth,       CreatureType.Wind),        ReactionEffect.Sandstorm   },
            { (CreatureType.Wind,        CreatureType.Earth),       ReactionEffect.Sandstorm   },
            { (CreatureType.Earth,       CreatureType.Electricity), ReactionEffect.Magnetize   },
            { (CreatureType.Electricity, CreatureType.Earth),       ReactionEffect.Magnetize   },
            { (CreatureType.Wind,        CreatureType.Electricity), ReactionEffect.Lightning   },
            { (CreatureType.Electricity, CreatureType.Wind),        ReactionEffect.Lightning   },
        };

        public static ReactionEffect GetReaction(CreatureType attacker, CreatureType defender)
            => _table.TryGetValue((attacker, defender), out ReactionEffect effect)
               ? effect
               : ReactionEffect.None;
    }
}
