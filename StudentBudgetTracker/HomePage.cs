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
        private ListView lstExpenses;
        private ListView lstGoals;
        private ListView lstRecurring;
        private Button btnDeleteExpense;
        private Button btnDeleteGoal;
        private Button btnDeleteRecurring;
        private Timer _recurringTimer;

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

            comboBox1.SelectedIndexChanged += comboBox1_SelectedIndexChanged;
            BuildListPanels();
            LoadDashboard();
            LoadCategories();
            LoadExpensesAndUpdateCharts();
            ProcessRecurringExpenses();
            RefreshExpenseList();
            RefreshGoalsList();
            RefreshRecurringList();
            CheckLimitAlerts();

            // Process recurring expenses every hour (matches the web app)
            _recurringTimer = new Timer();
            _recurringTimer.Interval = 3600000;
            _recurringTimer.Tick += (s, ev) => ProcessRecurringExpenses();
            _recurringTimer.Start();
            this.FormClosing += (s, ev) =>
            {
                if (_recurringTimer != null)
                {
                    _recurringTimer.Stop();
                    _recurringTimer.Dispose();
                }
            };
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
                string symbol = GetCurrencySymbol(_budget != null ? _budget.Currency : "USD");

                label16.Text = $"{symbol}{todayTotal:F2}";
                label15.Text = $"{symbol}{weekTotal:F2}";
                label17.Text = $"{symbol}{monthTotal:F2}";
                label21.Text = $"{symbol}{remaining:F2}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Summary error: {ex.Message}");
            }
        }

        private string GetCurrencySymbol(string currency)
        {
            switch (currency)
            {
                case "NGN": return "₦";
                case "USD": return "$";
                case "EUR": return "€";
                case "GBP": return "£";
                case "JPY": return "¥";
                case "CAD": return "C$";
                case "AUD": return "A$";
                case "INR": return "₹";
                default: return "$";
            }
        }

        private string FormatMoney(decimal amount)
        {
            string symbol = GetCurrencySymbol(_budget != null ? _budget.Currency : "USD");
            return $"{symbol}{amount:F2}";
        }

        private void CheckLimitAlerts()
        {
            try
            {
                if (_budget == null) return;

                decimal todayTotal = _db.GetTodayTotal(_currentUser.UserId);
                decimal weekTotal = _db.GetWeekTotal(_currentUser.UserId);
                var messages = new List<string>();

                if (_budget.DailyLimit > 0 && todayTotal > _budget.DailyLimit)
                    messages.Add($"Daily limit exceeded! Spent {FormatMoney(todayTotal)} (Limit: {FormatMoney(_budget.DailyLimit)})");

                if (_budget.WeeklyLimit > 0 && weekTotal > _budget.WeeklyLimit)
                    messages.Add($"Weekly limit exceeded! Spent {FormatMoney(weekTotal)} (Limit: {FormatMoney(_budget.WeeklyLimit)})");

                if (messages.Count > 0)
                    MessageBox.Show(string.Join("\n", messages), "Budget Alert",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Alert error: {ex.Message}");
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (comboBox1.SelectedItem == null) return;
                if (_budget == null) _budget = new BudgetSetting { UserId = _currentUser.UserId };
                _budget.Currency = comboBox1.SelectedItem.ToString();
                if (_db.GetBudgetSetting(_currentUser.UserId) == null)
                    _db.CreateBudgetSetting(_budget);
                else
                    _db.UpdateBudgetSetting(_budget);
                UpdateSummaryCards();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Currency error: {ex.Message}");
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

                string symbol = GetCurrencySymbol(_budget != null ? _budget.Currency : "USD");
                chart2.ChartAreas[0].AxisY.LabelStyle.Format = symbol + "{0:F2}";

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
                        warningMessage += $"Daily Limit Exceeded!\n" +
                                         $"   Limit: {FormatMoney(budget.DailyLimit)}\n" +
                                         $"   Spent today: {FormatMoney(todayTotal)}\n" +
                                         $"   This expense: {FormatMoney(expenseAmount)}\n" +
                                         $"   Would exceed by: {FormatMoney(exceededBy)}\n\n";
                    }
                }

                // ====== CHECK WEEKLY LIMIT ======
                if (budget.WeeklyLimit > 0)
                {
                    decimal weekTotal = _db.GetWeekTotal(_currentUser.UserId);

                    if (weekTotal + expenseAmount > budget.WeeklyLimit)
                    {
                        decimal exceededBy = (weekTotal + expenseAmount) - budget.WeeklyLimit;
                        warningMessage += $"Weekly Limit Exceeded!\n" +
                                         $"   Limit: {FormatMoney(budget.WeeklyLimit)}\n" +
                                         $"   Spent this week: {FormatMoney(weekTotal)}\n" +
                                         $"   This expense: {FormatMoney(expenseAmount)}\n" +
                                         $"   Would exceed by: {FormatMoney(exceededBy)}\n\n";
                    }
                }

                // ====== CHECK MONTHLY INCOME ======
                if (budget.MonthlyIncome > 0)
                {
                    decimal monthTotal = _db.GetMonthTotal(_currentUser.UserId);

                    if (monthTotal + expenseAmount > budget.MonthlyIncome)
                    {
                        decimal exceededBy = (monthTotal + expenseAmount) - budget.MonthlyIncome;
                        warningMessage += $"Monthly Budget Exceeded!\n" +
                                         $"   Budget: {FormatMoney(budget.MonthlyIncome)}\n" +
                                         $"   Spent this month: {FormatMoney(monthTotal)}\n" +
                                         $"   This expense: {FormatMoney(expenseAmount)}\n" +
                                         $"   Would exceed by: {FormatMoney(exceededBy)}\n\n";
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

                // Refresh charts, summary and expense list
                RefreshAllCharts();
                UpdateSummaryCards();
                RefreshExpenseList();

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

                RefreshGoalsList();
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

                RefreshRecurringList();
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

                    RefreshExpenseList();
                    RefreshGoalsList();
                    RefreshRecurringList();

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
        // 8. LIST PANELS (Recent Expenses, Savings Goals, Recurring)
        // ============================================================
        private void BuildListPanels()
        {
            // ---- Recent Expenses ----
            var panelExpenses = new Panel();
            panelExpenses.BackColor = Color.White;
            panelExpenses.BorderStyle = BorderStyle.FixedSingle;
            panelExpenses.Location = new Point(16, 1312);
            panelExpenses.Size = new Size(733, 300);
            panelExpenses.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            var lblExpensesHeader = new Label();
            lblExpensesHeader.Text = "Recent Expenses";
            lblExpensesHeader.Font = new Font("Microsoft Sans Serif", 12F, FontStyle.Bold);
            lblExpensesHeader.Location = new Point(12, 10);
            lblExpensesHeader.Size = new Size(300, 20);

            lstExpenses = new ListView();
            lstExpenses.View = View.Details;
            lstExpenses.FullRowSelect = true;
            lstExpenses.GridLines = true;
            lstExpenses.Location = new Point(12, 40);
            lstExpenses.Size = new Size(705, 200);
            lstExpenses.Columns.Add("Category", 200);
            lstExpenses.Columns.Add("Amount", 130);
            lstExpenses.Columns.Add("Date", 120);
            lstExpenses.Columns.Add("Type", 90);

            btnDeleteExpense = new Button();
            btnDeleteExpense.Text = "Delete Selected";
            btnDeleteExpense.BackColor = Color.Red;
            btnDeleteExpense.ForeColor = Color.White;
            btnDeleteExpense.Location = new Point(600, 258);
            btnDeleteExpense.Size = new Size(117, 30);
            btnDeleteExpense.Click += DeleteSelectedExpense;

            panelExpenses.Controls.Add(lblExpensesHeader);
            panelExpenses.Controls.Add(lstExpenses);
            panelExpenses.Controls.Add(btnDeleteExpense);

            // ---- Savings Goals ----
            var panelGoals = new Panel();
            panelGoals.BackColor = Color.White;
            panelGoals.BorderStyle = BorderStyle.FixedSingle;
            panelGoals.Location = new Point(16, 1622);
            panelGoals.Size = new Size(733, 260);
            panelGoals.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            var lblGoalsHeader = new Label();
            lblGoalsHeader.Text = "Savings Goals";
            lblGoalsHeader.Font = new Font("Microsoft Sans Serif", 12F, FontStyle.Bold);
            lblGoalsHeader.Location = new Point(12, 10);
            lblGoalsHeader.Size = new Size(300, 20);

            lstGoals = new ListView();
            lstGoals.View = View.Details;
            lstGoals.FullRowSelect = true;
            lstGoals.GridLines = true;
            lstGoals.Location = new Point(12, 40);
            lstGoals.Size = new Size(705, 160);
            lstGoals.Columns.Add("Goal Name", 160);
            lstGoals.Columns.Add("Target", 120);
            lstGoals.Columns.Add("Current", 120);
            lstGoals.Columns.Add("Progress", 90);
            lstGoals.Columns.Add("Deadline", 100);

            btnDeleteGoal = new Button();
            btnDeleteGoal.Text = "Delete Selected";
            btnDeleteGoal.BackColor = Color.Red;
            btnDeleteGoal.ForeColor = Color.White;
            btnDeleteGoal.Location = new Point(600, 216);
            btnDeleteGoal.Size = new Size(117, 30);
            btnDeleteGoal.Click += DeleteSelectedGoal;

            panelGoals.Controls.Add(lblGoalsHeader);
            panelGoals.Controls.Add(lstGoals);
            panelGoals.Controls.Add(btnDeleteGoal);

            // ---- Recurring Expenses ----
            var panelRecurring = new Panel();
            panelRecurring.BackColor = Color.White;
            panelRecurring.BorderStyle = BorderStyle.FixedSingle;
            panelRecurring.Location = new Point(16, 1892);
            panelRecurring.Size = new Size(733, 260);
            panelRecurring.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            var lblRecurringHeader = new Label();
            lblRecurringHeader.Text = "Recurring Expenses";
            lblRecurringHeader.Font = new Font("Microsoft Sans Serif", 12F, FontStyle.Bold);
            lblRecurringHeader.Location = new Point(12, 10);
            lblRecurringHeader.Size = new Size(300, 20);

            lstRecurring = new ListView();
            lstRecurring.View = View.Details;
            lstRecurring.FullRowSelect = true;
            lstRecurring.GridLines = true;
            lstRecurring.Location = new Point(12, 40);
            lstRecurring.Size = new Size(705, 160);
            lstRecurring.Columns.Add("Category", 200);
            lstRecurring.Columns.Add("Amount", 130);
            lstRecurring.Columns.Add("Frequency", 110);
            lstRecurring.Columns.Add("Start Date", 120);

            btnDeleteRecurring = new Button();
            btnDeleteRecurring.Text = "Delete Selected";
            btnDeleteRecurring.BackColor = Color.Red;
            btnDeleteRecurring.ForeColor = Color.White;
            btnDeleteRecurring.Location = new Point(600, 216);
            btnDeleteRecurring.Size = new Size(117, 30);
            btnDeleteRecurring.Click += DeleteSelectedRecurring;

            panelRecurring.Controls.Add(lblRecurringHeader);
            panelRecurring.Controls.Add(lstRecurring);
            panelRecurring.Controls.Add(btnDeleteRecurring);

            this.Controls.Add(panelExpenses);
            this.Controls.Add(panelGoals);
            this.Controls.Add(panelRecurring);
        }

        private void RefreshExpenseList()
        {
            _expenses = _db.GetExpenses(_currentUser.UserId, 100);
            lstExpenses.Items.Clear();
            foreach (var exp in _expenses)
            {
                var item = new ListViewItem(exp.CategoryName ?? "Other");
                item.SubItems.Add(FormatMoney(exp.Amount));
                item.SubItems.Add(exp.ExpenseDate);
                item.SubItems.Add(exp.IsRecurring ? "Recurring" : "One-time");
                item.Tag = exp.ExpenseId;
                lstExpenses.Items.Add(item);
            }
        }

        private void RefreshGoalsList()
        {
            var goals = _db.GetSavingsGoals(_currentUser.UserId);
            lstGoals.Items.Clear();
            foreach (var goal in goals)
            {
                var item = new ListViewItem(goal.GoalName);
                item.SubItems.Add(FormatMoney(goal.TargetAmount));
                item.SubItems.Add(FormatMoney(goal.CurrentAmount));
                item.SubItems.Add($"{goal.Progress:F0}%");
                item.SubItems.Add(string.IsNullOrEmpty(goal.Deadline) ? "-" : goal.Deadline);
                item.Tag = goal.GoalId;
                lstGoals.Items.Add(item);
            }
        }

        private void RefreshRecurringList()
        {
            var recurrings = _db.GetRecurringExpenses(_currentUser.UserId);
            lstRecurring.Items.Clear();
            foreach (var rec in recurrings)
            {
                var item = new ListViewItem(rec.CategoryName ?? "Other");
                item.SubItems.Add(FormatMoney(rec.Amount));
                item.SubItems.Add(rec.Frequency);
                item.SubItems.Add(rec.StartDate);
                item.Tag = rec.RecurringId;
                lstRecurring.Items.Add(item);
            }
        }

        private void DeleteSelectedExpense(object sender, EventArgs e)
        {
            if (lstExpenses.SelectedItems.Count == 0)
            {
                MessageBox.Show("Please select an expense to delete.", "Delete",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (MessageBox.Show("Delete the selected expense?", "Confirm",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                _db.DeleteExpense((int)lstExpenses.SelectedItems[0].Tag);
                RefreshExpenseList();
                RefreshAllCharts();
                UpdateSummaryCards();
            }
        }

        private void DeleteSelectedGoal(object sender, EventArgs e)
        {
            if (lstGoals.SelectedItems.Count == 0)
            {
                MessageBox.Show("Please select a savings goal to delete.", "Delete",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (MessageBox.Show("Delete the selected savings goal?", "Confirm",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                _db.DeleteSavingsGoal((int)lstGoals.SelectedItems[0].Tag);
                RefreshGoalsList();
            }
        }

        private void DeleteSelectedRecurring(object sender, EventArgs e)
        {
            if (lstRecurring.SelectedItems.Count == 0)
            {
                MessageBox.Show("Please select a recurring expense to delete.", "Delete",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (MessageBox.Show("Delete the selected recurring expense?", "Confirm",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                _db.DeleteRecurringExpense((int)lstRecurring.SelectedItems[0].Tag);
                RefreshRecurringList();
            }
        }

        // ============================================================
        // 9. RECURRING EXPENSE PROCESSING
        // ============================================================
        private void ProcessRecurringExpenses()
        {
            try
            {
                var recurrings = _db.GetRecurringExpenses(_currentUser.UserId);
                string today = DateTime.Now.ToString("yyyy-MM-dd");
                bool changed = false;

                foreach (var rec in recurrings)
                {
                    if (!rec.IsActive) continue;
                    if (!ShouldProcessRecurring(rec, today)) continue;

                    _db.AddExpense(new Expense
                    {
                        UserId = _currentUser.UserId,
                        CategoryId = rec.CategoryId,
                        Amount = rec.Amount,
                        ExpenseDate = today,
                        Description = "Recurring",
                        IsRecurring = true,
                        RecurringId = rec.RecurringId
                    });
                    _db.UpdateRecurringLastProcessed(rec.RecurringId, today);
                    changed = true;
                }

                if (changed)
                {
                    RefreshAllCharts();
                    RefreshExpenseList();
                    UpdateSummaryCards();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error processing recurring expenses: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool ShouldProcessRecurring(RecurringExpense rec, string today)
        {
            if (string.IsNullOrEmpty(rec.LastProcessed)) return true;

            DateTime last = DateTime.Parse(rec.LastProcessed);
            DateTime current = DateTime.Parse(today);
            int daysDiff = (int)(current - last).TotalDays;

            switch (rec.Frequency)
            {
                case "daily": return daysDiff >= 1;
                case "weekly": return daysDiff >= 7;
                case "monthly": return daysDiff >= 30;
                default: return false;
            }
        }

        // ============================================================
        // 10. DESIGNER EVENT HANDLERS - At the bottom
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

        private void label16_Click_1(object sender, EventArgs e)
        {

        }
    }
}