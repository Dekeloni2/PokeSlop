namespace FinalProject.Data
{
    // What USE does with an item. Consumables are spent; armor is equipped and
    // swaps with whatever was already worn. There's deliberately no weapon
    // kind — the player's attack is fixed.
    public enum ItemKind
    {
        Consumable,
        Armor
    }
}
