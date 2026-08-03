using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FinalProject.Data
{
    // loads the item list from JSON. All items live in one file since they
    // aren't tied to a specific teacher.
    public static class ItemLoader
    {
        public static List<ItemData> LoadAll(string jsonPath)
        {
            string json = File.ReadAllText(jsonPath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                // so "kind": "armor" reads without having to match the casing
                Converters = { new JsonStringEnumConverter() }
            };

            List<ItemJson> data = JsonSerializer.Deserialize<List<ItemJson>>(json, options)
                ?? throw new Exception($"Empty or invalid item JSON: {jsonPath}");

            var items = new List<ItemData>();
            foreach (ItemJson i in data)
                items.Add(new ItemData(i.Name, i.Description, i.HealAmount, i.Price, i.Kind, i.Defense));
            return items;
        }

        private class ItemJson
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public int HealAmount { get; set; }
            public int Price { get; set; }

            // omitting "kind" leaves an item a consumable, so the existing
            // food entries keep working untouched
            public ItemKind Kind { get; set; } = ItemKind.Consumable;
            public int Defense { get; set; }
        }
    }
}
