using System;
using System.Collections.Generic;
using FinalProject.Battle.Patterns;

namespace FinalProject.Battle
{
    // maps the "pattern" string from the teacher JSON to the actual pattern
    // class. New patterns need an entry here to be usable from JSON.
    //
    // TESTING: cut down to yakir's lesson arc plus the one he only throws if
    // you provoke him. the rest are still in Battle/Patterns, add their lines
    // back when you're done:
    //   Rain, ExpandingBox, Boat, Napoleon, Hexagon
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
        };

        public static Func<IBulletPattern> Resolve(string name)
        {
            if (Patterns.TryGetValue(name, out Func<IBulletPattern> factory))
                return factory;

            throw new ArgumentException($"Unknown bullet pattern \"{name}\", add it to PatternRegistry.");
        }
    }
}
