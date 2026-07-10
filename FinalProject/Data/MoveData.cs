using System;
using FinalProject.Battle;

namespace FinalProject.Data
{
    // data for one enemy move
    public class MoveData
    {
        public string      Name        { get; }
        public int         Power       { get; }    // 0 = status move, no damage
        public int         Accuracy    { get; }    // 0-100
        public string      Description { get; }

        // factory for this move's bullet pattern. Each turn gets a fresh
        // instance since patterns keep their own spawn timers
        public Func<IBulletPattern> CreatePattern { get; }

        public bool IsStatusMove => Power == 0;

        public MoveData(string name, int power, int accuracy, string description,
            Func<IBulletPattern> createPattern)
        {
            Name          = name;
            Power         = power;
            Accuracy      = accuracy;
            Description   = description;
            CreatePattern = createPattern;
        }
    }
}
