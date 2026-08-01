using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace FinalProject.Data
{
    // loads the item list from JSON. All items live in one file since they
    // aren't tied to a specific teacher.
    public static class ItemLoader
    {
        public static List<ItemData> LoadAll(string jsonPath)
        {
            string json = File.ReadAllText(jsonPath);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            List<ItemJson> data = JsonSerializer.Deserialize<List<ItemJson>>(json, options)
                ?? throw new Exception($"Empty or invalid item JSON: {jsonPath}");

            var items = new List<ItemData>();
            foreach (ItemJson i in data)
                items.Add(new ItemData(i.Name, i.Description, i.HealAmount, i.Price));
            return items;
        }

        private class ItemJson
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public int HealAmount { get; set; }
            public int Price { get; set; }
        }
    }
}
