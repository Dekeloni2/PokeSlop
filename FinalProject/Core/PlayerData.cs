using System;
using System.Collections.Generic;
using FinalProject.Data;

namespace FinalProject.Core
{
    // the player's stats and inventory
    public class PlayerData
    {
        public int CurrentHp { get; set; } = 100;
        public int MaxHp     { get; set; } = 100;
        public int Attack    { get; set; } = 10;
        public int Money     { get; set; } = 500;

        // no stacking, using an item just removes its entry
        public List<ItemData> Inventory { get; } = new();

        public bool IsAlive => CurrentHp > 0;

        public void TakeDamage(int amount) => CurrentHp = Math.Max(0, CurrentHp - amount);
        public void Heal(int amount)       => CurrentHp = Math.Min(MaxHp, CurrentHp + amount);
    }
}
