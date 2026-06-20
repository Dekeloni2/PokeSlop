using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace FinalProject.World
{
    // Reads a Tiled JSON export and constructs a TileMap ready for use.
    // Usage: TileMap map = MapLoader.Load("Content/Maps/town_1.json", Content);
    public static class MapLoader
    {
        public static TileMap Load(string jsonPath, ContentManager content)
        {
            string json = File.ReadAllText(jsonPath);

            using JsonDocument doc  = JsonDocument.Parse(json);
            JsonElement        root = doc.RootElement;

            int mapWidth   = root.GetProperty("width").GetInt32();
            int mapHeight  = root.GetProperty("height").GetInt32();
            int tileWidth  = root.GetProperty("tilewidth").GetInt32();
            int tileHeight = root.GetProperty("tileheight").GetInt32();

            string mapDir = Path.GetDirectoryName(Path.GetFullPath(jsonPath));

            // ── Tilesets ─────────────────────────────────────────────────────
            var tilesets = new List<TilesetInfo>();

            foreach (JsonElement tsEl in root.GetProperty("tilesets").EnumerateArray())
            {
                int firstGid = tsEl.GetProperty("firstgid").GetInt32();

                TilesetInfo info = tsEl.TryGetProperty("source", out JsonElement sourceEl)
                    ? LoadExternalTileset(Path.Combine(mapDir, sourceEl.GetString()), firstGid, content)
                    : ParseTileset(tsEl, firstGid, content, mapDir);

                if (info != null)
                    tilesets.Add(info);
            }

            // Must be sorted ascending by firstGid for FindTileset to work correctly
            tilesets.Sort((a, b) => a.FirstGid.CompareTo(b.FirstGid));

            // ── Layers ───────────────────────────────────────────────────────
            TileLayer groundLayer  = null;
            TileLayer objectsLayer = null;

            foreach (JsonElement layerEl in root.GetProperty("layers").EnumerateArray())
            {
                string type = layerEl.GetProperty("type").GetString();

                // Skip anything that isn't a tile layer
                if (type != "tilelayer") continue;

                string name        = layerEl.GetProperty("name").GetString();
                int    layerWidth  = layerEl.GetProperty("width").GetInt32();
                int    layerHeight = layerEl.GetProperty("height").GetInt32();

                int[] data = ParseLayerData(layerEl, layerWidth * layerHeight);
                var   layer = new TileLayer(name, layerWidth, layerHeight, data);

                if (name == "Ground")  groundLayer  = layer;
                if (name == "Objects") objectsLayer = layer;
            }

            if (groundLayer == null)
                throw new Exception("Map is missing a layer named 'Ground'.");
            if (objectsLayer == null)
                throw new Exception("Map is missing a layer named 'Objects'.");

            return new TileMap(mapWidth, mapHeight, tileWidth, tileHeight,
                               tilesets, groundLayer, objectsLayer);
        }

        // ── Private helpers ──────────────────────────────────────────────────

        private static TilesetInfo LoadExternalTileset(string tsPath, int firstGid, ContentManager content)
        {
            string json = File.ReadAllText(tsPath);
            using JsonDocument doc = JsonDocument.Parse(json);
            string mapDir = Path.GetDirectoryName(tsPath);
            return ParseTileset(doc.RootElement, firstGid, content, mapDir);
        }

        private static TilesetInfo ParseTileset(JsonElement el, int firstGid, ContentManager content, string mapDir)
        {
            string imagePath = el.GetProperty("image").GetString();
            int    columns   = el.GetProperty("columns").GetInt32();
            int    tileWidth  = el.GetProperty("tilewidth").GetInt32();
            int    tileHeight = el.GetProperty("tileheight").GetInt32();

            string    contentKey = ImagePathToContentKey(imagePath, mapDir);

            // Debug: record attempted content key loads to a file so we can inspect them
            try
            {
                string logPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "map_debug.txt"));
                File.AppendAllText(logPath, $"Attempting to load texture key: {contentKey}\n");
            }
            catch { /* ignore logging failures */ }

            Texture2D texture;
            try
            {
                texture = content.Load<Texture2D>(contentKey);
            }
            catch (Exception ex)
            {
                try
                {
                    string logPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "map_debug.txt"));
                    File.AppendAllText(logPath, $"Failed to load texture '{contentKey}': {ex.Message}\n");
                }
                catch { }

                // Rethrow so callers can handle the failure
                throw;
            }

            return new TilesetInfo(firstGid, texture, columns, tileWidth, tileHeight);
        }

        private static int[] ParseLayerData(JsonElement layerEl, int count)
        {
            int[] data = new int[count];
            int   i    = 0;

            foreach (JsonElement val in layerEl.GetProperty("data").EnumerateArray())
                data[i++] = val.GetInt32();

            return data;
        }

        // Converts a Tiled image path to a ContentManager key.
        // "../../Content/Sprites/Tilesets/TileMap.png" → "Sprites/Tilesets/TileMap"
        // Resolves image paths relative to the map file location and converts them to
        // ContentManager keys (path without extension, using forward slashes).
        private static string ImagePathToContentKey(string imagePath, string mapDir)
        {
            // Resolve the image path relative to the map file's directory
            string fullPath = Path.GetFullPath(Path.Combine(mapDir ?? string.Empty, imagePath));
            string normalized = fullPath.Replace('\\', '/');

            // Find "Content/" and take everything after it as the key
            int index = normalized.IndexOf("/Content/", StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                string relative = normalized.Substring(index + "/Content/".Length);
                // Ensure forward slashes and remove extension
                string key = Path.ChangeExtension(relative, null).Replace('\\', '/');
                // If the key still starts with an extra Content/ prefix, strip it
                if (key.StartsWith("Content/", StringComparison.OrdinalIgnoreCase))
                    key = key.Substring("Content/".Length);
                if (key.StartsWith("/Content/", StringComparison.OrdinalIgnoreCase))
                    key = key.Substring("/Content/".Length);
                return key;
            }

            // If we couldn't find Content/, fall back to using the filename without extension
            string fallback = Path.GetFileNameWithoutExtension(imagePath);
            if (fallback.StartsWith("Content/", StringComparison.OrdinalIgnoreCase))
                fallback = fallback.Substring("Content/".Length);
            return fallback;
        }
    }
}