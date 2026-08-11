using System;
using System.Collections.Generic;

namespace FinalProject.Core
{
    // Where the hand-authored JSON lives — teachers, maps, items, endings,
    // game over scenarios. These are read directly rather than through the
    // content pipeline's ContentManager, so they need their own answer to
    // "where's Content" at runtime.
    //
    // Everything here is a *relative* path with forward slashes, because
    // that's the one spelling both platforms agree on. ContentFiles hands it
    // to TitleContainer, which resolves it against the folder holding the exe
    // on desktop and against the page's base URL in the browser — see
    // ContentFiles for why that's the only file access the web build has.
    //
    // Note none of this touches the filesystem, on purpose: under WebAssembly
    // there isn't one to touch. Path.GetFullPath in particular would resolve
    // against a working directory that doesn't exist in the browser, which is
    // why Combine/Normalize below do their own '.'/'..' folding in string
    // space instead of asking the OS.
    public static class ContentPaths
    {
        public const string Root = "Content";

        // e.g. Under("Teachers", "yakir.json") -> "Content/Teachers/yakir.json"
        public static string Under(params string[] parts)
        {
            string path = Root;
            foreach (string part in parts)
                path = Combine(path, part);
            return path;
        }

        // Joins two path fragments with '/', letting an absolute-ish second
        // fragment win, then folds away any '.' / '..' segments.
        public static string Combine(string directory, string relative)
        {
            if (string.IsNullOrEmpty(relative)) return Normalize(directory ?? string.Empty);
            if (string.IsNullOrEmpty(directory)) return Normalize(relative);

            relative = relative.Replace('\\', '/');
            if (relative.StartsWith("/", StringComparison.Ordinal))
                return Normalize(relative);

            return Normalize(directory.Replace('\\', '/').TrimEnd('/') + "/" + relative);
        }

        // "Content/Maps/entrance.tmj" -> "Content/Maps". Empty when there's no
        // directory part at all.
        public static string DirectoryOf(string path)
        {
            if (string.IsNullOrEmpty(path)) return string.Empty;

            string normalized = path.Replace('\\', '/');
            int slash = normalized.LastIndexOf('/');
            return slash < 0 ? string.Empty : normalized.Substring(0, slash);
        }

        // Resolves '.' and '..' segments without consulting the filesystem.
        // A leading '..' that can't be folded away is kept rather than
        // silently dropped, so a wrong path stays visibly wrong.
        public static string Normalize(string path)
        {
            if (string.IsNullOrEmpty(path)) return string.Empty;

            string normalized = path.Replace('\\', '/');
            bool rooted = normalized.StartsWith("/", StringComparison.Ordinal);

            var segments = new List<string>();
            foreach (string segment in normalized.Split('/'))
            {
                if (segment.Length == 0 || segment == ".")
                    continue;

                if (segment == ".." && segments.Count > 0 && segments[segments.Count - 1] != "..")
                {
                    segments.RemoveAt(segments.Count - 1);
                    continue;
                }

                segments.Add(segment);
            }

            return (rooted ? "/" : string.Empty) + string.Join("/", segments);
        }
    }
}
