using System.Collections.Generic;
using FinalProject.Battle;

namespace FinalProject.Core
{
    public class PlayerData
    {
        public List<Creature> Party { get; } = new();
        public int Money { get; set; } = 500;
    }
}
