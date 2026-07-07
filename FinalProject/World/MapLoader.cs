using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Xml.Linq;
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
            TileLayer tallgrass = null;
            var interactables = new List<Interactable>();

            foreach (JsonElement layerEl in root.GetProperty("layers").EnumerateArray())
            {
                string type = layerEl.GetProperty("type").GetString();

                // Object layers (Tiled's "objectgroup") hold freeform placed
                // objects rather than a tile grid — that's where interactables
                // (signs, NPCs, anything with per-instance text) live.
                if (type == "objectgroup")
                {
                    string groupName = layerEl.GetProperty("name").GetString();
                    if (groupName == "Interactables")
                        interactables.AddRange(ParseInteractables(layerEl, tileWidth, tileHeight));
                    continue;
                }

                // Skip anything that isn't a tile layer
                if (type != "tilelayer") continue;

                string name        = layerEl.GetProperty("name").GetString();
                int    layerWidth  = layerEl.GetProperty("width").GetInt32();
                int    layerHeight = layerEl.GetProperty("height").GetInt32();

                int[] data = ParseLayerData(layerEl, layerWidth * layerHeight);
                var   layer = new TileLayer(name, layerWidth, layerHeight, data);

                if (name == "Ground")  groundLayer  = layer;
                if (name == "Objects") objectsLayer = layer;
                if (name == "Tall Grass") tallgrass = layer;
            }

            if (groundLayer == null)
                throw new Exception("Map is missing a layer named 'Ground'.");
            if (objectsLayer == null)
                throw new Exception("Map is missing a layer named 'Objects'.");

            // tallgrass is optional; pass it through to the TileMap so the game
            // can render it and query for tall-grass tiles.
            return new TileMap(mapWidth, mapHeight, tileWidth, tileHeight,
                               tilesets, groundLayer, tallgrass, objectsLayer, interactables);
        }

        // Reads every object in an "Interactables" object layer into a list of
        // Interactable instances. Each object's pixel rect is converted to the
        // tile range it covers, and its "Text" custom property (set in Tiled's
        // Properties panel) becomes the dialogue shown on interact.
        private static List<Interactable> ParseInteractables(JsonElement layerEl, int tileWidth, int tileHeight)
        {
            var results = new List<Interactable>();

            if (!layerEl.TryGetProperty("objects", out JsonElement objectsEl))
                return results;

            foreach (JsonElement objEl in objectsEl.EnumerateArray())
            {
                double x      = objEl.GetProperty("x").GetDouble();
                double y      = objEl.GetProperty("y").GetDouble();
                double width  = objEl.TryGetProperty("width",  out JsonElement wEl) ? wEl.GetDouble() : 0;
                double height = objEl.TryGetProperty("height", out JsonElement hEl) ? hEl.GetDouble() : 0;

                int minTileX = (int)Math.Floor(x / tileWidth);
                int minTileY = (int)Math.Floor(y / tileHeight);
                int maxTileX = Math.Max(minTileX, (int)Math.Ceiling((x + width)  / tileWidth)  - 1);
                int maxTileY = Math.Max(minTileY, (int)Math.Ceiling((y + height) / tileHeight) - 1);

                string text = "";
                if (objEl.TryGetProperty("properties", out JsonElement propsEl))
                {
                    foreach (JsonElement propEl in propsEl.EnumerateArray())
                    {
                        if (propEl.GetProperty("name").GetString() == "Text")
                        {
                            text = propEl.GetProperty("value").GetString();
                            break;
                        }
                    }
                }

                results.Add(new Interactable(minTileX, minTileY, maxTileX, maxTileY, text));
            }

            return results;
        }

        // ── Private helpers ──────────────────────────────────────────────────

        // An external tileset reference can point at either Tiled's native XML
        // format (.tsx — the default when you save a tileset outside the map)
        // or a JSON tileset (.tsj, from exporting as JSON). Branch on extension.
        private static TilesetInfo LoadExternalTileset(string tsPath, int firstGid, ContentManager content)
        {
            string mapDir = Path.GetDirectoryName(tsPath);

            if (string.Equals(Path.GetExtension(tsPath), ".tsx", StringComparison.OrdinalIgnoreCase))
                return ParseTilesetXml(tsPath, firstGid, content, mapDir);

            string json = File.ReadAllText(tsPath);
            using JsonDocument doc = JsonDocument.Parse(json);
            return ParseTileset(doc.RootElement, firstGid, content, mapDir);
        }

        private static TilesetInfo ParseTileset(JsonElement el, int firstGid, ContentManager content, string mapDir)
        {
            string imagePath = el.GetProperty("image").GetString();
            int    columns   = el.GetProperty("columns").GetInt32();
            int    tileWidth  = el.GetProperty("tilewidth").GetInt32();
            int    tileHeight = el.GetProperty("tileheight").GetInt32();

            string contentKey = ImagePathToContentKey(imagePath, mapDir);
            Texture2D texture = LoadTexture(content, contentKey);

            return new TilesetInfo(firstGid, texture, columns, tileWidth, tileHeight);
        }

        // Parses Tiled's native .tsx (plain XML) tileset format:
        // <tileset ... tilewidth="20" tileheight="20" columns="32">
        //   <image source="../Content/Sprites/Tilesets/foo.png" .../>
        // </tileset>
        private static TilesetInfo ParseTilesetXml(string tsPath, int firstGid, ContentManager content, string mapDir)
        {
            XElement tilesetEl = XDocument.Load(tsPath).Root;

            int columns    = (int)tilesetEl.Attribute("columns");
            int tileWidth  = (int)tilesetEl.Attribute("tilewidth");
            int tileHeight = (int)tilesetEl.Attribute("tileheight");

            string imagePath = (string)tilesetEl.Element("image").Attribute("source");

            string contentKey = ImagePathToContentKey(imagePath, mapDir);
            Texture2D texture = LoadTexture(content, contentKey);

            return new TilesetInfo(firstGid, texture, columns, tileWidth, tileHeight);
        }

        // Loads a texture by ContentManager key, logging both the attempt and
        // any failure to map_debug.txt next to the executable — the only place
        // to look since this runs headless with no console attached.
        private static Texture2D LoadTexture(ContentManager content, string contentKey)
        {
            LogDebug($"Attempting to load texture key: {contentKey}");

            try
            {
                return content.Load<Texture2D>(contentKey);
            }
            catch (Exception ex)
            {
                LogDebug($"Failed to load texture '{contentKey}': {ex.Message}");
                throw; // rethrow so callers (MapLoader.Load) can handle the failure
            }
        }

        private static void LogDebug(string message)
        {
            try
            {
                string logPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "map_debug.txt"));
                File.AppendAllText(logPath, message + "\n");
            }
            catch { /* ignore logging failures */ }
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
