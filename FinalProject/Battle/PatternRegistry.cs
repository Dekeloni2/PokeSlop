using System;
using System.Collections.Generic;
using FinalProject.Battle.Patterns;

namespace FinalProject.Battle
{
    // maps the "pattern" string from the teacher JSON to the actual pattern
    // class. New patterns need an entry here to be usable from JSON.
    public static class PatternRegistry
    {
        private static readonly Dictionary<string, Func<IBulletPattern>> Patterns = new()
        {
            ["Rain"] = () => new RainPattern(), // RainPattern
            ["ExpandingBox"] = () => new ExpandingBoxPattern(), // ExpandingBoxPattern
            ["Boat"] = () => new BoatPattern(), // BoatPattern
            //["Napoleon"] = () => new NapoleonPattern(), // NapoleonPattern
            ["Vegeta"] = () => new GarlicGunPattern(), // GarlicGunPattern
        };

        public static Func<IBulletPattern> Resolve(string name)
        {
            if (Patterns.TryGetValue(name, out Func<IBulletPattern> factory))
                return factory;

            throw new ArgumentException($"Unknown bullet pattern \"{name}\", add it to PatternRegistry.");
        }
    }
}
