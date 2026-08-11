using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StudentBudgetTracker.Models
{
    public class SavingsGoal
    {
        public int GoalId { get; set; }
        public int UserId { get; set; }
        public string GoalName { get; set; }
        public decimal TargetAmount { get; set; }
        public decimal CurrentAmount { get; set; }
        public string Deadline { get; set; }
        public DateTime CreatedAt { get; set; }

        // Computed property
        public decimal Progress => TargetAmount > 0 ? (CurrentAmount / TargetAmount) * 100 : 0;
    }
}
