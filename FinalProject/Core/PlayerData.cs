using System;

namespace FinalProject.Core
{
    // The player's own battle-relevant state. This is an Undertale-style RPG —
    // one protagonist fighting Teacher bosses directly, not a party of
    // creatures — so this holds the player's own HP/Attack rather than a
    // roster of anything collectible.
    public class PlayerData
    {
        public int CurrentHp { get; set; } = 100;
        public int MaxHp     { get; set; } = 100;
        public int Attack    { get; set; } = 10;
        public int Money     { get; set; } = 500;

        public bool IsAlive => CurrentHp > 0;

        public void TakeDamage(int amount) => CurrentHp = Math.Max(0, CurrentHp - amount);
        public void Heal(int amount)       => CurrentHp = Math.Min(MaxHp, CurrentHp + amount);
    }
}
