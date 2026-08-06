namespace FinalProject.Data
{
    // an item in the bag. Consumables heal; armor is worn for its Defense.
    public class ItemData
    {
        public string   Name        { get; }
        public string   Description { get; }
        public int      HealAmount  { get; }
        public int      Price       { get; }
        public ItemKind Kind        { get; }

        // only meaningful on armor — how much it takes off each hit
        public int Defense { get; }

        // true for items that exist but aren't for sale (found, not bought —
        // the hall's pickable cheese is the first one). Game1.OpenVendingMachine
        // filters these out of what the shop shows; everything else about them
        // (healing, taking up bag space) works the same as a bought item
        public bool Hidden { get; }

        public bool IsEquipment => Kind == ItemKind.Armor;

        public ItemData(string name, string description, int healAmount, int price,
                        ItemKind kind = ItemKind.Consumable, int defense = 0, bool hidden = false)
        {
            Name        = name;
            Description = description;
            HealAmount  = healAmount;
            Price       = price;
            Kind        = kind;
            Defense     = defense;
            Hidden      = hidden;
        }
    }
}
