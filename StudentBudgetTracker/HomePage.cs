using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using StudentBudgetTracker.DAL;
using StudentBudgetTracker.Helpers;
using StudentBudgetTracker.Models;

namespace StudentBudgetTracker
{
    public partial class HomePage : Form
    {
        // ============================================================
        // 1. CLASS-LEVEL VARIABLES - At the top of the class
        // ============================================================
        private DatabaseHelper _db = new DatabaseHelper();
        private User _currentUser;
        private BudgetSetting _budget;
        private List<Expense> _expenses;

        // ============================================================
        // 2. CONSTRUCTOR
        // ============================================================
        public HomePage()
        {
            InitializeComponent();
            _currentUser = SessionManager.CurrentUser;

            if (_currentUser == null)
            {
                MessageBox.Show("Session expired. Please login again.", "Session Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.Close();
                return;
            }

            LoadDashboard();
            LoadCategories();
            LoadExpensesAndUpdateCharts();
            WireUpEvents();
        }

        // ============================================================
        // 3. WIRE UP EVENTS
        // ============================================================
        private void WireUpEvents()
        {
            button3.Click += button3_Click;
            button4.Click += button4_Click;
            button7.Click += button7_Click;
            button5.Click += button5_Click;
            button6.Click += button6_Click;
            button1.Click += button1_Click;
            button2.Click += button2_Click;
        }

        // ============================================================
        // 4. DASHBOARD METHODS
        // ============================================================
        private void LoadDashboard()
        {
            try
            {
                _budget = _db.GetBudgetSetting(_currentUser.UserId);
                if (_budget != null)
                {
                    textBox1.Text = _budget.MonthlyIncome.ToString();
                    textBox2.Text = _budget.DailyLimit.ToString();
                    textBox3.Text = _budget.WeeklyLimit.ToString();
                    int index = comboBox1.FindString(_budget.Currency ?? "USD");
                    if (index >= 0) comboBox1.SelectedIndex = index;
                }
                UpdateSummaryCards();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading dashboard: {ex.Message}", "Error");
            }
        }

        private void UpdateSummaryCards()
        {
            try
            {
                decimal todayTotal = _db.GetTodayTotal(_currentUser.UserId);
                decimal weekTotal = _db.GetWeekTotal(_currentUser.UserId);
                decimal monthTotal = _db.GetMonthTotal(_currentUser.UserId);
                decimal remaining = _budget != null ? _budget.MonthlyIncome - monthTotal : 0;

                label14.Text = $"${todayTotal:F2}";
                label18.Text = $"${weekTotal:F2}";
                label20.Text = $"${monthTotal:F2}";
                label19.Text = $"${remaining:F2}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Summary error: {ex.Message}");
            }
        }

        private void LoadCategories()
        {
            try
            {
                var categories = _db.GetCategories(_currentUser.UserId);
                comboBox4.DataSource = new List<Category>(categories);
                comboBox4.DisplayMember = "CategoryName";
                comboBox4.ValueMember = "CategoryId";

                comboBox2.DataSource = new List<Category>(categories);
                comboBox2.DisplayMember = "CategoryName";
                comboBox2.ValueMember = "CategoryId";

                comboBox3.Items.Clear();
                comboBox3.Items.AddRange(new object[] { "daily", "weekly", "monthly" });
                comboBox3.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading categories: {ex.Message}", "Error");
            }
        }

        // ============================================================
        // 5. CHART METHODS
        // ============================================================
        private void LoadExpensesAndUpdateCharts()
        {
            try
            {
                _expenses = _db.GetExpenses(_currentUser.UserId, 100);
                UpdateAllCharts();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading expenses: {ex.Message}", "Error");
            }
        }

        private void UpdateSplineChart()
        {
            try
            {
                chart2.Series[0].Points.Clear();
                chart2.Series[0].ChartType = SeriesChartType.Spline;
                chart2.Series[0].BorderWidth = 3;
                chart2.Series[0].Color = Color.DodgerBlue;
                chart2.Series[0].MarkerStyle = MarkerStyle.Circle;
                chart2.Series[0].MarkerSize = 6;

                for (int i = 6; i >= 0; i--)
                {
                    DateTime date = DateTime.Now.AddDays(-i);
                    string dateStr = date.ToString("yyyy-MM-dd");
                    string dayLabel = date.ToString("ddd");

                    decimal total = 0;
                    if (_expenses != null)
                    {
                        foreach (var exp in _expenses)
                        {
                            if (exp.ExpenseDate == dateStr)
                                total += exp.Amount;
                        }
                    }

                    chart2.Series[0].Points.AddXY(dayLabel, (double)total);
                }

                chart2.Invalidate();
                chart2.Refresh();
                chart2.Update();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating spline chart: {ex.Message}", "Error");
            }
        }

        private void UpdateDoughnutChart()
        {
            try
            {
                chart1.Series[0].Points.Clear();
                chart1.Series[0].ChartType = SeriesChartType.Doughnut;
                chart1.Series[0].BorderWidth = 1;
                chart1.Series[0].BorderColor = Color.White;
                chart1.Series[0].IsValueShownAsLabel = false;

                if (_expenses == null || _expenses.Count == 0)
                {
                    chart1.Series[0].Points.AddXY("No Data", 1);
                }
                else
                {
                    var categoryTotals = new Dictionary<string, decimal>();

                    foreach (var exp in _expenses)
                    {
                        string cat = string.IsNullOrEmpty(exp.CategoryName) ? "Uncategorized" : exp.CategoryName;
                        if (categoryTotals.ContainsKey(cat))
                            categoryTotals[cat] += exp.Amount;
                        else
                            categoryTotals.Add(cat, exp.Amount);
                    }

                    Color[] colors = new Color[]
                    {
                        Color.FromArgb(255, 99, 132),
                        Color.FromArgb(54, 162, 235),
                        Color.FromArgb(255, 206, 86),
                        Color.FromArgb(75, 192, 192),
                        Color.FromArgb(153, 102, 255),
                        Color.FromArgb(255, 159, 64)
                    };

                    int colorIndex = 0;
                    foreach (var item in categoryTotals)
                    {
                        int pointIndex = chart1.Series[0].Points.AddXY(item.Key, (double)item.Value);
                        if (colorIndex < colors.Length)
                            chart1.Series[0].Points[pointIndex].Color = colors[colorIndex];
                        colorIndex++;
                    }
                }

                chart1.Invalidate();
                chart1.Refresh();
                chart1.Update();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating doughnut chart: {ex.Message}", "Error");
            }
        }

        private void UpdateAllCharts()
        {
            UpdateSplineChart();
            UpdateDoughnutChart();
        }

        private void RefreshAllCharts()
        {
            _expenses = _db.GetExpenses(_currentUser.UserId, 100);
            UpdateAllCharts();
        }

        // ============================================================
        // 6. BUDGET LIMIT CHECKER - PLACE HERE (After chart methods)
        // ============================================================

        /// <summary>
        /// Checks if an expense exceeds daily, weekly, or monthly limits
        /// </summary>
        /// <param name="expenseAmount">The amount of the expense</param>
        /// <param name="expenseDate">The date of the expense</param>
        /// <returns>Warning message if any limit is exceeded, empty string if all good</returns>
        private string CheckBudgetLimits(decimal expenseAmount, string expenseDate)
        {
            string warningMessage = "";

            try
            {
                var budget = _db.GetBudgetSetting(_currentUser.UserId);
                if (budget == null) return ""; // No budget set

                // ====== CHECK DAILY LIMIT ======
                if (budget.DailyLimit > 0)
                {
                    decimal todayTotal = _db.GetTodayTotal(_currentUser.UserId);

                    if (todayTotal + expenseAmount > budget.DailyLimit)
                    {
                        decimal exceededBy = (todayTotal + expenseAmount) - budget.DailyLimit;
                        warningMessage += $"🔴 Daily Limit Exceeded!\n" +
                                         $"   Limit: ${budget.DailyLimit:F2}\n" +
                                         $"   Spent today: ${todayTotal:F2}\n" +
                                         $"   This expense: ${expenseAmount:F2}\n" +
                                         $"   Would exceed by: ${exceededBy:F2}\n\n";
                    }
                }

                // ====== CHECK WEEKLY LIMIT ======
                if (budget.WeeklyLimit > 0)
                {
                    decimal weekTotal = _db.GetWeekTotal(_currentUser.UserId);

                    if (weekTotal + expenseAmount > budget.WeeklyLimit)
                    {
                        decimal exceededBy = (weekTotal + expenseAmount) - budget.WeeklyLimit;
                        warningMessage += $"🔴 Weekly Limit Exceeded!\n" +
                                         $"   Limit: ${budget.WeeklyLimit:F2}\n" +
                                         $"   Spent this week: ${weekTotal:F2}\n" +
                                         $"   This expense: ${expenseAmount:F2}\n" +
                                         $"   Would exceed by: ${exceededBy:F2}\n\n";
                    }
                }

                // ====== CHECK MONTHLY INCOME ======
                if (budget.MonthlyIncome > 0)
                {
                    decimal monthTotal = _db.GetMonthTotal(_currentUser.UserId);

                    if (monthTotal + expenseAmount > budget.MonthlyIncome)
                    {
                        decimal exceededBy = (monthTotal + expenseAmount) - budget.MonthlyIncome;
                        warningMessage += $"🔴 Monthly Budget Exceeded!\n" +
                                         $"   Budget: ${budget.MonthlyIncome:F2}\n" +
                                         $"   Spent this month: ${monthTotal:F2}\n" +
                                         $"   This expense: ${expenseAmount:F2}\n" +
                                         $"   Would exceed by: ${exceededBy:F2}\n\n";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error checking limits: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return warningMessage;
        }

        // ============================================================
        // 7. BUTTON EVENT HANDLERS - PLACE HERE (After CheckBudgetLimits)
        // ============================================================

        // ====== ADD EXPENSE BUTTON ======
        private void button7_Click(object sender, EventArgs e)  // Add Expense
        {
            try
            {
                // Validate amount
                if (!decimal.TryParse(textBox7.Text, out decimal amount) || amount <= 0)
                {
                    MessageBox.Show("Please enter a valid amount greater than 0.", "Validation",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Validate category
                if (comboBox4.SelectedItem == null)
                {
                    MessageBox.Show("Please select a category.", "Validation",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // ====== CHECK BUDGET LIMITS ======
                string expenseDate = dateTimePicker3.Value.ToString("yyyy-MM-dd");
                string warningMessage = CheckBudgetLimits(amount, expenseDate);

                // ====== IF WARNING EXISTS, ASK USER ======
                if (!string.IsNullOrEmpty(warningMessage))
                {
                    DialogResult result = MessageBox.Show(
                        $"⚠️ BUDGET WARNING!\n\n" +
                        $"{warningMessage}\n" +
                        $"Do you still want to add this expense?",
                        "Budget Limit Warning",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);

                    // If user clicks No, cancel the expense
                    if (result == DialogResult.No)
                    {
                        textBox7.Clear();
                        return;
                    }
                    // If user clicks Yes, continue to add expense
                }

                // ====== ADD EXPENSE ======
                var selectedCategory = (Category)comboBox4.SelectedItem;
                var expense = new Expense
                {
                    UserId = _currentUser.UserId,
                    CategoryId = selectedCategory.CategoryId,
                    Amount = amount,
                    ExpenseDate = expenseDate,
                    Description = "",
                    IsRecurring = false,
                    RecurringId = null
                };

                _db.AddExpense(expense);
                textBox7.Clear();

                // Refresh charts and summary
                RefreshAllCharts();
                UpdateSummaryCards();

                // Show success message
                if (!string.IsNullOrEmpty(warningMessage))
                {
                    MessageBox.Show("✅ Expense added successfully!\n\n" +
                                   "⚠️ Note: This expense exceeded your budget limit(s).",
                                   "Expense Added",
                                   MessageBoxButtons.OK,
                                   MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("✅ Expense added successfully!",
                        "Success",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding expense: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ====== SET INCOME BUTTON ======
        private void button3_Click(object sender, EventArgs e)
        {
            try
            {
                if (decimal.TryParse(textBox1.Text, out decimal income) && income >= 0)
                {
                    if (_budget == null) _budget = new BudgetSetting { UserId = _currentUser.UserId };
                    _budget.MonthlyIncome = income;

                    if (_db.GetBudgetSetting(_currentUser.UserId) == null)
                        _db.CreateBudgetSetting(_budget);
                    else
                        _db.UpdateBudgetSetting(_budget);

                    LoadDashboard();
                    MessageBox.Show("Income updated successfully!", "Success",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Please enter a valid income amount.", "Validation",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ====== SET LIMITS BUTTON ======
        private void button4_Click(object sender, EventArgs e)
        {
            try
            {
                bool dailyValid = decimal.TryParse(textBox2.Text, out decimal daily);
                bool weeklyValid = decimal.TryParse(textBox3.Text, out decimal weekly);

                if (dailyValid && weeklyValid && daily >= 0 && weekly >= 0)
                {
                    if (_budget == null) _budget = new BudgetSetting { UserId = _currentUser.UserId };
                    _budget.DailyLimit = daily;
                    _budget.WeeklyLimit = weekly;

                    if (_db.GetBudgetSetting(_currentUser.UserId) == null)
                        _db.CreateBudgetSetting(_budget);
                    else
                        _db.UpdateBudgetSetting(_budget);

                    LoadDashboard();
                    MessageBox.Show("Budget limits updated successfully!", "Success",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Please enter valid limit amounts (0 or greater).", "Validation",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ====== ADD GOAL BUTTON ======
        private void button5_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(textBox5.Text))
                {
                    MessageBox.Show("Please enter a goal name.", "Validation",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!decimal.TryParse(textBox4.Text, out decimal target) || target <= 0)
                {
                    MessageBox.Show("Please enter a valid target amount greater than 0.", "Validation",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var goal = new SavingsGoal
                {
                    UserId = _currentUser.UserId,
                    GoalName = textBox5.Text,
                    TargetAmount = target,
                    CurrentAmount = 0,
                    Deadline = dateTimePicker1.Value.ToString("yyyy-MM-dd")
                };

                _db.AddSavingsGoal(goal);
                textBox5.Clear();
                textBox4.Clear();

                MessageBox.Show("Savings goal added successfully!", "Success",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding goal: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ====== ADD RECURRING BUTTON ======
        private void button6_Click(object sender, EventArgs e)
        {
            try
            {
                if (!decimal.TryParse(textBox6.Text, out decimal amount) || amount <= 0)
                {
                    MessageBox.Show("Please enter a valid amount greater than 0.", "Validation",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (comboBox2.SelectedItem == null)
                {
                    MessageBox.Show("Please select a category.", "Validation",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (comboBox3.SelectedItem == null)
                {
                    MessageBox.Show("Please select a frequency.", "Validation",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var selectedCategory = (Category)comboBox2.SelectedItem;
                var recurring = new RecurringExpense
                {
                    UserId = _currentUser.UserId,
                    CategoryId = selectedCategory.CategoryId,
                    Amount = amount,
                    Frequency = comboBox3.SelectedItem.ToString(),
                    StartDate = dateTimePicker2.Value.ToString("yyyy-MM-dd"),
                    IsActive = true
                };

                _db.AddRecurringExpense(recurring);
                textBox6.Clear();

                MessageBox.Show("Recurring expense added successfully!", "Success",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding recurring expense: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ====== RESET ALL BUTTON ======
        private void button1_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show(
                "⚠️ Are you sure you want to reset ALL data?\n\nThis cannot be undone!",
                "Confirm Reset",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                try
                {
                    _db.ResetAllData(_currentUser.UserId);
                    RefreshAllCharts();
                    UpdateSummaryCards();
                    LoadDashboard();

                    textBox1.Clear();
                    textBox2.Clear();
                    textBox3.Clear();
                    textBox4.Clear();
                    textBox5.Clear();
                    textBox6.Clear();
                    textBox7.Clear();

                    MessageBox.Show("All data has been reset successfully!", "Success",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error resetting data: {ex.Message}", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // ====== LOGOUT BUTTON ======
        private void button2_Click(object sender, EventArgs e)
        {
            SessionManager.Logout();
            this.Close();
        }

        // ============================================================
        // 8. DESIGNER EVENT HANDLERS - At the bottom
        // ============================================================
        private void button1_Click_1(object sender, EventArgs e) { button1_Click(sender, e); }
        private void button2_Click_1(object sender, EventArgs e) { button2_Click(sender, e); }
        private void chart1_Click(object sender, EventArgs e) { }
        private void chart2_Click(object sender, EventArgs e) { }
        private void panel2_Paint(object sender, PaintEventArgs e) { }
        private void panel9_Paint(object sender, PaintEventArgs e) { }
        private void label16_Click(object sender, EventArgs e) { }
        private void label17_Click(object sender, EventArgs e) { }
        private void HomePage_Load(object sender, EventArgs e) { }
    }
}