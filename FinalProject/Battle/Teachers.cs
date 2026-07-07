using System;
using System.Collections.Generic;
using FinalProject.Data;
namespace FinalProject.Battle;

// A boss Teacher, built from a TeacherStats blueprint. This is the runtime
// instance BattleState fights — tracks the mutable stuff (current HP,
// whether it's been spared) while Stats holds the fixed blueprint (max HP,
// attack, moves).
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

    // TODO: real mercy logic — right now this just flags the boss as spared.
    // Undertale-style Spare usually only succeeds after enough ACT progress;
    // that gating isn't modeled yet (see BattleState).
    public void Spare() => IsSpared = true;
}
