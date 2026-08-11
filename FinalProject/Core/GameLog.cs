using System;
using System.IO;

namespace FinalProject.Core
{
    // Where "something went wrong loading content" goes. MapLoader,
    // OverworldState and EndingConfig each used to keep their own private copy
    // of this, all appending to the same map_debug.txt — one place now, since
    // the answer to "where does a log line go" is different per platform and
    // shouldn't be re-derived three times.
    //
    // Desktop keeps the file next to the exe, because a windowed game has no
    // console attached to read. The browser can't write that file anywhere the
    // player could find it, so the console line is the real output there — it
    // shows up in devtools. Writing both costs nothing and means neither
    // platform is the one without logs.
    public static class GameLog
    {
        public const string FileName = "map_debug.txt";

        public static void Write(string message)
        {
            Console.WriteLine(message);

            // Best effort. Under WebAssembly this lands in an in-memory
            // filesystem nobody can read, and on a read-only install it throws
            // outright — neither is worth taking the game down for.
            try   { File.AppendAllText(FileName, message + "\n"); }
            catch { }
        }
    }
}
