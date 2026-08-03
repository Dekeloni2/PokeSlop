using System;
using System.Collections.Generic;
using FinalProject.Data;
using FinalProject.Events;

namespace FinalProject.Core
{
    // the player's stats and inventory
    public class PlayerData
    {
        // shown in the overworld menu's name box and on the STAT screen
        public string Name { get; set; } = "CLOVER";

        public int CurrentHp { get; set; } = 20;
        public int MaxHp     { get; set; } = 20;
        public int Attack    { get; set; } = 10;
        public int Money     { get; set; } = 10;

        // Undertale caps the bag at 8. The overworld menu draws that many rows
        // and the shop refuses to sell past it.
        public const int InventoryCapacity = 8;

        // no stacking, using an item just removes its entry
        public List<ItemData> Inventory { get; } = new();

        public bool IsInventoryFull => Inventory.Count >= InventoryCapacity;

        public bool IsAlive => CurrentHp > 0;

        // ── Equipment ────────────────────────────────────────────────────────
        // Armor only — the player's attack is fixed, so there's no weapon slot.
        // What's worn is out of the bag, so it can't be dropped by accident.
        public ItemData EquippedArmor { get; private set; }

        public int BaseDefense   { get; set; } = 0;
        public int ArmorDefense  => EquippedArmor?.Defense ?? 0;
        public int Defense       => BaseDefense + ArmorDefense;

        // Equipping swaps in place: the armor leaves the bag and whatever was
        // worn takes its slot, so the bag count and ordering hold steady.
        public void EquipArmorFromInventory(int index)
        {
            if (index < 0 || index >= Inventory.Count) return;

            ItemData armor = Inventory[index];
            if (!armor.IsEquipment) return;

            ItemData previous = EquippedArmor;
            EquippedArmor = armor;

            if (previous == null) Inventory.RemoveAt(index);
            else                  Inventory[index] = previous;
        }

        // HP only changes through these two, so this is the one place that fires
        // the event. the battle HUD subscribes to it instead of reading the value
        // every frame
        public void TakeDamage(int amount)
        {
            if (amount <= 0) return;

            // armor takes the edge off, but never all of it — a floor of 1 stops
            // stacked defense from making the player unkillable
            int taken = Math.Max(1, amount - Defense);

            CurrentHp = Math.Max(0, CurrentHp - taken);
            EventBus.Instance.Publish(new PlayerHpChangedEvent(CurrentHp, MaxHp));
        }

        public void Heal(int amount)
        {
            CurrentHp = Math.Min(MaxHp, CurrentHp + amount);
            EventBus.Instance.Publish(new PlayerHpChangedEvent(CurrentHp, MaxHp));
        }
    }
}
