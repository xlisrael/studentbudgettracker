using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StudentBudgetTracker.Models
{
    public class BudgetSetting
    {
        public int SettingId { get; set; }
        public int UserId { get; set; }
        public decimal MonthlyIncome { get; set; }
        public decimal DailyLimit { get; set; }
        public decimal WeeklyLimit { get; set; }
        public string Currency { get; set; }
    }
}
