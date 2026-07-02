using System;
using System.Collections.Generic;
using FinalProject.Data;
namespace FinalProject.Battle;

public class Teacher
{
    public TeacherStats TeacherName { get; set; }
    public int CurrentHp { get; set; }
    public int MaxHp { get; set; }
    public int Attack {get; set;}

    public IReadOnlyList<MoveData> Moves { get; set; }
    
    public bool IsSpared = false;
    
    public Teacher(TeacherStats teacherName, int level)
        {
        TeacherName = teacherName;
        CurrentHp = MaxHp;
        Moves = BuildAttackList();
        }
    
    public void TakeDamage(int amount)
    => CurrentHp = Math.Max(0, CurrentHp - amount);
    
    public void Heal(int amount)
    => CurrentHp = Math.Min(MaxHp, CurrentHp + amount);

    private List<MoveData> BuildAttackList()
    {
        var hasAttack = new List<MoveData>();
        int start = Math.Max(0, hasAttack.Count - 4);
        return hasAttack.GetRange(start, hasAttack.Count - start );
    }
}