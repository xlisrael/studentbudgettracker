using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StudentBudgetTracker.Models
{
    public class RecurringExpense
    {
        public int RecurringId { get; set; }
        public int UserId { get; set; }
        public int CategoryId { get; set; }
        public decimal Amount { get; set; }
        public string Frequency { get; set; } // 'daily', 'weekly', 'monthly'
        public string StartDate { get; set; }
        public string LastProcessed { get; set; }
        public bool IsActive { get; set; }
        public string CategoryName { get; set; }
        // Removed CreatedAt - it doesn't exist in the table
    }
}