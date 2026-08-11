using System;
using System.IO;
using System.Text;
using System.Xml.Linq;
using Microsoft.Xna.Framework;

namespace FinalProject.Core
{
    // The one door every hand-authored data file goes through — maps, teacher
    // stats, items, endings, warps, boss gates.
    //
    // It exists because System.IO.File has nothing to open in the browser.
    // A WebAssembly build has no real filesystem: the game's data files are
    // just URLs sitting next to index.html, and the only way to read one is an
    // HTTP request. TitleContainer is KNI's abstraction over exactly that —
    // File.OpenRead against the folder holding the exe on desktop, a
    // synchronous XMLHttpRequest against the page's base URL in the browser.
    // Same call, same relative path, both platforms, and crucially still
    // *synchronous*, so none of the loaders had to become async.
    //
    // Paths are relative and use forward slashes; see ContentPaths.
    public static class ContentFiles
    {
        public static Stream OpenRead(string relativePath)
            => TitleContainer.OpenStream(ContentPaths.Normalize(relativePath));

        public static string ReadAllText(string relativePath)
        {
            using (Stream stream = OpenRead(relativePath))
            using (var reader = new StreamReader(stream, Encoding.UTF8))
                return reader.ReadToEnd();
        }

        public static XDocument ReadXml(string relativePath)
        {
            using (Stream stream = OpenRead(relativePath))
                return XDocument.Load(stream);
        }

        // There's no cheap stat() to ask over HTTP — a missing file is a 404,
        // which TitleContainer surfaces as an IOException, the same way a
        // missing file on disk surfaces as a FileNotFoundException. So
        // "does it exist" is "did opening it throw", for both platforms.
        //
        // That makes this more expensive on the web than a File.Exists ever
        // was on desktop, so callers that are about to read the file anyway
        // should just call ReadAllText inside a try/catch instead of asking
        // first. This is for the genuinely optional files (a map's warps or
        // boss gates) where absence is normal and means "none".
        public static bool Exists(string relativePath)
        {
            try
            {
                using (Stream stream = OpenRead(relativePath))
                    return stream != null;
            }
            catch (IOException)          { return false; } // includes FileNotFound/DirectoryNotFound
            catch (UnauthorizedAccessException) { return false; }
        }

        // Reads a file that's allowed not to be there. Returns null when it
        // isn't, rather than making the caller pay for a separate Exists probe
        // (which on the web means fetching the whole file twice).
        public static string ReadAllTextOrNull(string relativePath)
        {
            try                     { return ReadAllText(relativePath); }
            catch (IOException)     { return null; }
            catch (UnauthorizedAccessException) { return null; }
        }
    }
}
