using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StudentBudgetTracker.Models
{
    public class Expense
    {
        public int ExpenseId { get; set; }
        public int UserId { get; set; }
        public int CategoryId { get; set; }
        public decimal Amount { get; set; }
        public string ExpenseDate { get; set; }
        public string Description { get; set; }
        public bool IsRecurring { get; set; }
        public int? RecurringId { get; set; }
        public DateTime CreatedAt { get; set; }

        // For display purposes
        public string CategoryName { get; set; }
    }
}
