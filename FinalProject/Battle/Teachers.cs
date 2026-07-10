using System;
using System.Collections.Generic;
using FinalProject.Data;
namespace FinalProject.Battle;

// a boss during battle. TeacherStats holds the fixed data (loaded from JSON),
// this tracks what changes during the fight - HP, spare progress etc.
public class Teacher
{
    public TeacherStats Stats { get; }
    public string Name => Stats.Name;

    public int CurrentHp { get; private set; }
    public int MaxHp     { get; }
    public int Attack    { get; }

    public IReadOnlyList<MoveData> Moves { get; }

    public bool IsSpared { get; private set; }

    public bool IsAlive => CurrentHp > 0;

    // mercy progress built up by ACT options. BattleState checks this
    // against Stats.SpareSuccessAt before calling Spare()
    public int SparePercent { get; private set; }

    public Teacher(TeacherStats stats)
    {
        Stats     = stats;
        MaxHp     = stats.BaseHp;
        CurrentHp = MaxHp;
        Attack    = stats.BaseAtk;
        Moves     = stats.Moves;
    }

    public void TakeDamage(int amount)
        => CurrentHp = Math.Max(0, CurrentHp - amount);

    public void Heal(int amount)
        => CurrentHp = Math.Min(MaxHp, CurrentHp + amount);

    public void IncreaseSparePercent(int amount)
        => SparePercent = Math.Clamp(SparePercent + amount, 0, 100);

    public void Spare() => IsSpared = true;
}
