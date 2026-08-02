using System;
using System.Collections.Generic;
using FinalProject.Battle.Patterns;

namespace FinalProject.Battle
{
    // maps the "pattern" string from the teacher JSON to the actual pattern
    // class. New patterns need an entry here to be usable from JSON.
    //
    // TESTING: yakir's lesson arc, the one he only throws if you provoke him,
    // and the two the substitute is being used to test. the rest are still in
    // Battle/Patterns, add their lines back when you need them:
    //   Rain, ExpandingBox
    public static class PatternRegistry
    {
        private static readonly Dictionary<string, Func<IBulletPattern>> Patterns = new()
        {
            ["DataTypes"]    = () => new DataTypePattern(),
            ["Conditionals"] = () => new ConditionalPattern(),
            ["Loops"]        = () => new LoopPattern(),
            ["Program"]      = () => new ProgramPattern(),
            ["Pixel"]        = () => new PixelPattern(),   // yakir's ultimate
            ["GarlicGun"]    = () => new GarlicGunPattern(), // only if you bring up the photo
            ["Napoleon"]     = () => new NapoleonPattern(),
            ["Hexagon"]      = () => new HexagonPattern(),
            ["Onion"] = () => new OnionPattern(),
            ["AllStar"] = () => new AllStarPattern(), // david's ultimate
            ["Punch"]        = () => new PunchPattern(),
            ["Boat"]         = () => new BoatPattern(),
            ["Cloverbyte"]   = () => new CloverbytePattern(),
            ["Chess"]        = () => new ChessPattern(),

            // the training dummy's three lessons — one class, one entry per
            // rule it teaches. see TrainingLinePattern
            ["TrainingWhite"]  = () => new TrainingLinePattern(HazardRule.White),
            ["TrainingBlue"]   = () => new TrainingLinePattern(HazardRule.Blue),
            ["TrainingOrange"] = () => new TrainingLinePattern(HazardRule.Orange),
        };

        public static Func<IBulletPattern> Resolve(string name)
        {
            if (Patterns.TryGetValue(name, out Func<IBulletPattern> factory))
                return factory;

            throw new ArgumentException($"Unknown bullet pattern \"{name}\", add it to PatternRegistry.");
        }
    }
}
