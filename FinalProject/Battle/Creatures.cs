using System;
using System.Collections.Generic;
using FinalProject.Data;
namespace FinalProject.Battle;

public class Creature
{
    public CreatureSpecies Species { get; set; }
    public int Level { get; set; }
    public int CurrentHp { get; set; }
    public int MaxHp { get; set; }
    public int Attack {get; set;}
    public int Defense { get; set; }
    public int Speed {get; set;}
    public IReadOnlyList<MoveData> Moves { get; set; }
    public ReactionEffect ActiveEffect { get; set; }
    
    public bool IsAlive => CurrentHp > 0;
    public bool IsBurned => ActiveEffect == ReactionEffect.Inferno;
    public bool IsParalyzed => ActiveEffect == ReactionEffect.Lightning;
    public bool IsFrozen => ActiveEffect == ReactionEffect.Frost;
    
    
    
    public Creature(CreatureSpecies species, int level)
        {
        Species = species;
        Level = level;
        CalculateStats();
        CurrentHp = MaxHp;
        Moves = BuildMoveList();
        ActiveEffect = ReactionEffect.None;
        }
    
    public void TakeDamage(int amount)
    => CurrentHp = Math.Max(0, CurrentHp - amount);
    
    public void Heal(int amount)
    => CurrentHp = Math.Min(MaxHp, CurrentHp + amount);

    public void ApplyEffect(ReactionEffect effect)
    {
        // First effect sticks - don't overwrite an existing one
        if (ActiveEffect == ReactionEffect.None)
        {
            ActiveEffect = effect;
        }
    }
    
    public void ClearEffect() => ActiveEffect = ReactionEffect.None;

    private void CalculateStats()
    {
        MaxHp = (2 * Species.BaseHp * Level / 100) + Level + 10;
        Attack = (2 * Species.BaseAtk * Level / 100) + 5;
        Defense = (2 * Species.BaseDef * Level / 100) + 5;
        Speed = (2 * Species.BaseSpeed * Level / 100) + 5;
    }

    private List<MoveData> BuildMoveList()
    {
        var learned = new List<MoveData>();
        foreach (var (learnLevel, moveName) in Species.Learnset)
        {
            if (learnLevel <= Level)
            {
                var move = MoveRegistry.Get(moveName);
                learned.Add(move);
            }
        }
        int start = Math.Max(0, learned.Count - 4);
        return learned.GetRange(start, learned.Count - start );
    }
}