namespace FinalProject.Data
{
    // a usable item. Only healing for now, other effects would go here later.
    public class ItemData
    {
        public string Name        { get; }
        public string Description { get; }
        public int    HealAmount  { get; }

        public ItemData(string name, string description, int healAmount)
        {
            Name        = name;
            Description = description;
            HealAmount  = healAmount;
        }
    }
}
