using System.Collections.Generic;

namespace FinalProject.Data
{
    // Central registry of every creature species in the game.
    // Look up a species by ID: SpeciesRegistry.Get(1) → Embersaur
    public static class SpeciesRegistry
    {
        private static readonly Dictionary<int, CreatureSpecies> _all = new();

        public static CreatureSpecies Get(int id) => _all[id];
        public static IEnumerable<CreatureSpecies> All => _all.Values;

        static SpeciesRegistry()
        {
            // ── Fire line ────────────────────────────────────────────────────
            Register(new CreatureSpecies(
                id: 1, name: "Embersaur", type: CreatureType.Fire,
                spriteName: "Sprites/Creatures/fire_36",
                evolvesIntoId: 2,
                baseHp: 45, baseAtk: 60, baseDef: 49, baseSpAtk: 65, baseSpDef: 45, baseSpeed: 45,
                learnset: new (int, string)[]
                {
                    (1,  "Ember"),
                    (7,  "Scratch"),
                    (13, "Flame Burst"),
                    (20, "Fire Fang")
                }
            ));

            Register(new CreatureSpecies(
                id: 2, name: "Blazesaur", type: CreatureType.Fire,
                spriteName: "Sprites/Creatures/fire_36_2",
                evolvesIntoId: 3,
                baseHp: 60, baseAtk: 80, baseDef: 63, baseSpAtk: 80, baseSpDef: 60, baseSpeed: 60,
                learnset: new (int, string)[]
                {
                    (1,  "Ember"),
                    (1,  "Scratch"),
                    (18, "Flamethrower"),
                    (26, "Fire Blast")
                }
            ));

            Register(new CreatureSpecies(
                id: 3, name: "Infernosaur", type: CreatureType.Fire,
                spriteName: "Sprites/Creatures/fire_36_3",
                evolvesIntoId: -1,
                baseHp: 80, baseAtk: 100, baseDef: 83, baseSpAtk: 100, baseSpDef: 80, baseSpeed: 80,
                learnset: new (int, string)[]
                {
                    (1,  "Ember"),
                    (1,  "Scratch"),
                    (1,  "Flamethrower"),
                    (36, "Inferno")
                }
            ));

            // ── Water line ───────────────────────────────────────────────────
            Register(new CreatureSpecies(
                id: 4, name: "Spiritine", type: CreatureType.Water,
                spriteName: "Sprites/Creatures/water_37",
                evolvesIntoId: 5,
                baseHp: 44, baseAtk: 48, baseDef: 65, baseSpAtk: 50, baseSpDef: 64, baseSpeed: 43,
                learnset: new (int, string)[]
                {
                    (1,  "Water Gun"),
                    (7,  "Bubble"),
                    (13, "Aqua Jet"),
                    (20, "Surf")
                }
            ));

            Register(new CreatureSpecies(
                id: 5, name: "Spritino", type: CreatureType.Water,
                spriteName: "Sprites/Creatures/water_37_2",
                evolvesIntoId: 6,
                baseHp: 59, baseAtk: 63, baseDef: 80, baseSpAtk: 65, baseSpDef: 80, baseSpeed: 58,
                learnset: new (int, string)[]
                {
                    (1,  "Water Gun"),
                    (1,  "Bubble"),
                    (18, "Surf"),
                    (26, "Hydro Pump")
                }
            ));

            Register(new CreatureSpecies(
                id: 6, name: "Spiriariuas", type: CreatureType.Water,
                spriteName: "Sprites/Creatures/water_37_3",
                evolvesIntoId: -1,
                baseHp: 79, baseAtk: 83, baseDef: 100, baseSpAtk: 85, baseSpDef: 105, baseSpeed: 78,
                learnset: new (int, string)[]
                {
                    (1,  "Water Gun"),
                    (1,  "Surf"),
                    (1,  "Hydro Pump"),
                    (36, "Tidal Wave")
                }
            ));

            // ── Earth line ───────────────────────────────────────────────────
            Register(new CreatureSpecies(
                id: 7, name: "Grondi", type: CreatureType.Earth,
                spriteName: "Sprites/Creatures/rock_25",
                evolvesIntoId: 8,
                baseHp: 56, baseAtk: 61, baseDef: 65, baseSpAtk: 48, baseSpDef: 45, baseSpeed: 40,
                learnset: new (int, string)[]
                {
                    (1,  "Mud Shot"),
                    (7,  "Rock Throw"),
                    (13, "Bulldoze"),
                    (20, "Earthquake")
                }
            ));

            Register(new CreatureSpecies(
                id: 8, name: "Groundier", type: CreatureType.Earth,
                spriteName: "Sprites/Creatures/rock_25_2",
                evolvesIntoId: 9,
                baseHp: 72, baseAtk: 78, baseDef: 85, baseSpAtk: 60, baseSpDef: 60, baseSpeed: 52,
                learnset: new (int, string)[]
                {
                    (1,  "Mud Shot"),
                    (1,  "Rock Throw"),
                    (18, "Earthquake"),
                    (26, "Stone Edge")
                }
            ));

            Register(new CreatureSpecies(
                id: 9, name: "Groundizen", type: CreatureType.Earth,
                spriteName: "Sprites/Creatures/rock_25_3",
                evolvesIntoId: -1,
                baseHp: 94, baseAtk: 100, baseDef: 108, baseSpAtk: 76, baseSpDef: 80, baseSpeed: 68,
                learnset: new (int, string)[]
                {
                    (1,  "Mud Shot"),
                    (1,  "Earthquake"),
                    (1,  "Stone Edge"),
                    (36, "Tectonic Rage")
                }
            ));

            // ── Electricity line ─────────────────────────────────────────────
            Register(new CreatureSpecies(
                id: 10, name: "Sparkery", type: CreatureType.Electricity,
                spriteName: "Sprites/Creatures/electric_25",
                evolvesIntoId: 11,
                baseHp: 45, baseAtk: 50, baseDef: 45, baseSpAtk: 60, baseSpDef: 50, baseSpeed: 65,
                learnset: new (int, string)[]
                {
                    (1,  "Spark"),
                    (7,  "Thunder Shock"),
                    (13, "Charge Beam"),
                    (20, "Thunderbolt")
                }
            ));

            Register(new CreatureSpecies(
                id: 11, name: "Zappdery", type: CreatureType.Electricity,
                spriteName: "Sprites/Creatures/electric_25_2",
                evolvesIntoId: 12,
                baseHp: 60, baseAtk: 65, baseDef: 60, baseSpAtk: 80, baseSpDef: 65, baseSpeed: 85,
                learnset: new (int, string)[]
                {
                    (1,  "Spark"),
                    (1,  "Thunder Shock"),
                    (18, "Thunderbolt"),
                    (26, "Thunder")
                }
            ));

            Register(new CreatureSpecies(
                id: 12, name: "Wattaton", type: CreatureType.Electricity,
                spriteName: "Sprites/Creatures/electric_25_3",
                evolvesIntoId: -1,
                baseHp: 80, baseAtk: 82, baseDef: 78, baseSpAtk: 104, baseSpDef: 80, baseSpeed: 108,
                learnset: new (int, string)[]
                {
                    (1,  "Spark"),
                    (1,  "Thunderbolt"),
                    (1,  "Thunder"),
                    (36, "Gigavolt")
                }
            ));

            // ── Wind line ────────────────────────────────────────────────────
            Register(new CreatureSpecies(
                id: 13, name: "Okamery", type: CreatureType.Wind,
                spriteName: "Sprites/Creatures/flying_55",
                evolvesIntoId: 14,
                baseHp: 42, baseAtk: 56, baseDef: 40, baseSpAtk: 50, baseSpDef: 45, baseSpeed: 71,
                learnset: new (int, string)[]
                {
                    (1,  "Gust"),
                    (7,  "Wing Attack"),
                    (13, "Air Slash"),
                    (20, "Hurricane")
                }
            ));

            Register(new CreatureSpecies(
                id: 14, name: "Wolarine", type: CreatureType.Wind,
                spriteName: "Sprites/Creatures/flying_55_2",
                evolvesIntoId: 15,
                baseHp: 58, baseAtk: 74, baseDef: 55, baseSpAtk: 68, baseSpDef: 62, baseSpeed: 92,
                learnset: new (int, string)[]
                {
                    (1,  "Gust"),
                    (1,  "Wing Attack"),
                    (18, "Hurricane"),
                    (26, "Tempest")
                }
            ));

            Register(new CreatureSpecies(
                id: 15, name: "Cloudarie", type: CreatureType.Wind,
                spriteName: "Sprites/Creatures/flying_55_3",
                evolvesIntoId: -1,
                baseHp: 75, baseAtk: 95, baseDef: 70, baseSpAtk: 90, baseSpDef: 80, baseSpeed: 118,
                learnset: new (int, string)[]
                {
                    (1,  "Gust"),
                    (1,  "Hurricane"),
                    (1,  "Tempest"),
                    (36, "Sky Rend")
                }
            ));
        }

        private static void Register(CreatureSpecies species) => _all[species.Id] = species;
    }
}
