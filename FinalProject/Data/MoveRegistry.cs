using System.Collections.Generic;

namespace FinalProject.Data
{
    // Central registry of every move in the game.
    // Look up a move by name: MoveRegistry.Get("Ember") → MoveData
    // To add a new move, add one Register(...) line in the static constructor below.
    public static class MoveRegistry
    {
        private static readonly Dictionary<string, MoveData> _all = new();

        public static MoveData Get(string name) => _all[name];
        public static IEnumerable<MoveData> All => _all.Values;

        static MoveRegistry()
        {
            // ── Normal moves ─────────────────────────────────────────────────
            // These deal bonus damage when Crystalize is active on the target
            Register(new MoveData("Tackle",       CreatureType.Normal, 40,  100, "A basic full-body charge."));
            Register(new MoveData("Scratch",      CreatureType.Normal, 40,  100, "Rakes the target with sharp claws."));
            Register(new MoveData("Quick Attack", CreatureType.Normal, 40,  100, "An extremely fast strike that always hits first."));
            Register(new MoveData("Slam",         CreatureType.Normal, 85,  100, "Slams the target with full body weight. May inflict paralysis."));
            Register(new MoveData("Headbutt",     CreatureType.Normal, 70,  100, "Charges forward and slams with the head."));
            Register(new MoveData("Super Cannon", CreatureType.Normal, 150, 90,  "An overwhelming blast of raw energy. Makes the creature dormant for 1 turn."));

            // ── Fire moves ───────────────────────────────────────────────────
            Register(new MoveData("Ember",        CreatureType.Fire, 40,  100, "A weak fire attack."));
            Register(new MoveData("Flame Burst",  CreatureType.Fire, 70,  100, "A burst of flame."));
            Register(new MoveData("Fire Fang",    CreatureType.Fire, 65,  95,  "Bites with scorching fire. May inflict burn."));
            Register(new MoveData("Flamethrower", CreatureType.Fire, 90,  100, "A powerful stream of fire."));
            Register(new MoveData("Fire Blast",   CreatureType.Fire, 110, 85,  "A massive fire blast."));
            Register(new MoveData("Inferno",      CreatureType.Fire, 130, 75,  "An overwhelming inferno. Reduces this creature's attack."));

            // ── Water moves ──────────────────────────────────────────────────
            Register(new MoveData("Water Gun",  CreatureType.Water, 40,  100, "Shoots a stream of water."));
            Register(new MoveData("Bubble",     CreatureType.Water, 40,  100, "Fires a volley of bubbles."));
            Register(new MoveData("Aqua Jet",   CreatureType.Water, 40,  100, "A swift water strike."));
            Register(new MoveData("Surf",       CreatureType.Water, 90,  100, "A large wave crashes down."));
            Register(new MoveData("Hydro Pump", CreatureType.Water, 110, 80,  "A fierce jet of water."));
            Register(new MoveData("Tidal Wave", CreatureType.Water, 130, 75,  "An enormous tidal wave."));

            // ── Earth moves ──────────────────────────────────────────────────
            Register(new MoveData("Mud Shot",      CreatureType.Earth, 20,  100, "Flings mud at the target."));
            Register(new MoveData("Rock Throw",    CreatureType.Earth, 50,  90,  "Hurls a large rock."));
            Register(new MoveData("Bulldoze",      CreatureType.Earth, 60,  100, "Stomps the ground hard."));
            Register(new MoveData("Earthquake",    CreatureType.Earth, 100, 100, "Shakes the ground violently."));
            Register(new MoveData("Stone Edge",    CreatureType.Earth, 100, 80,  "Sharp stones strike the target."));
            Register(new MoveData("Tectonic Rage", CreatureType.Earth, 130, 75,  "A devastating seismic attack."));

            // ── Electricity moves ────────────────────────────────────────────
            Register(new MoveData("Spark",         CreatureType.Electricity, 40,  100, "A jolt of electricity."));
            Register(new MoveData("Thunder Shock",  CreatureType.Electricity, 40,  100, "A weak electric shock."));
            Register(new MoveData("Charge Beam",   CreatureType.Electricity, 50,  90,  "Fires a beam of electricity."));
            Register(new MoveData("Thunderbolt",   CreatureType.Electricity, 90,  100, "A strong electric blast."));
            Register(new MoveData("Thunder",       CreatureType.Electricity, 110, 70,  "A wild lightning strike."));
            Register(new MoveData("Gigavolt",      CreatureType.Electricity, 130, 75,  "An overwhelming surge of power."));

            // ── Wind moves ───────────────────────────────────────────────────
            Register(new MoveData("Gust",        CreatureType.Wind, 40,  100, "A sharp gust of wind."));
            Register(new MoveData("Wing Attack", CreatureType.Wind, 60,  100, "Strikes with sharp wings."));
            Register(new MoveData("Air Slash",   CreatureType.Wind, 75,  95,  "Slices with a blade of air."));
            Register(new MoveData("Hurricane",   CreatureType.Wind, 110, 70,  "A vicious spinning storm."));
            Register(new MoveData("Tempest",     CreatureType.Wind, 90,  100, "A raging windstorm."));
            Register(new MoveData("Sky Rend",    CreatureType.Wind, 130, 75,  "Tears through the sky itself."));
        }

        private static void Register(MoveData move) => _all[move.Name] = move;
    }
}
