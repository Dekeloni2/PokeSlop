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

        public bool IsEquipment => Kind == ItemKind.Armor;

        public ItemData(string name, string description, int healAmount, int price,
                        ItemKind kind = ItemKind.Consumable, int defense = 0)
        {
            Name        = name;
            Description = description;
            HealAmount  = healAmount;
            Price       = price;
            Kind        = kind;
            Defense     = defense;
        }
    }
}
