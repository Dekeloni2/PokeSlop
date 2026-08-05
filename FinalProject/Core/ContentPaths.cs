using System;
using System.IO;

namespace FinalProject.Core
{
    // Where the hand-authored JSON lives — teachers, maps, items, endings,
    // game over scenarios. These are read directly with File.ReadAllText
    // rather than through the content pipeline's ContentManager, so they
    // need their own answer to "where's Content" at runtime.
    //
    // It's copied to sit right next to the exe on every build (see the
    // <Content Include> rules in FinalProject.csproj) rather than being
    // found by walking up out of bin/Debug|Release/net9.0 into the
    // project's own source tree the way every one of these used to. That
    // only worked from inside a dev checkout — the moment the build folder
    // was moved, zipped up, or handed to someone else, it broke.
    public static class ContentPaths
    {
        public static string Root => Path.Combine(AppContext.BaseDirectory, "Content");

        // e.g. Under("Teachers", "yakir.json") or Under("Maps")
        public static string Under(params string[] parts)
        {
            var all = new string[parts.Length + 1];
            all[0] = Root;
            Array.Copy(parts, 0, all, 1, parts.Length);
            return Path.GetFullPath(Path.Combine(all));
        }
    }
}
