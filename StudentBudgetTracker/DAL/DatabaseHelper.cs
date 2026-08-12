using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using StudentBudgetTracker.Models;

namespace StudentBudgetTracker.DAL
{
    public class DatabaseHelper
    {
        private readonly string _connectionString;

        public DatabaseHelper()
        {
            _connectionString = @"Data Source=.\SQLEXPRESS;Initial Catalog=studentbudgetDB;Integrated Security=True;";
        }

        private SqlConnection GetConnection()
        {
            return new SqlConnection(_connectionString);
        }

        // ========== DATABASE INITIALIZATION ==========
        public void InitializeDatabase()
        {
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    string createSql = @"
                        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
                        BEGIN
                            CREATE TABLE Users (
                                UserId INT IDENTITY(1,1) PRIMARY KEY,
                                Username NVARCHAR(50) NOT NULL UNIQUE,
                                Email NVARCHAR(100) NOT NULL UNIQUE,
                                Password NVARCHAR(100) NOT NULL,
                                CreatedAt DATETIME DEFAULT GETDATE()
                            )
                            CREATE TABLE Categories (
                                CategoryId INT IDENTITY(1,1) PRIMARY KEY,
                                UserId INT NOT NULL,
                                CategoryName NVARCHAR(50) NOT NULL,
                                FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE
                            )
                            CREATE TABLE Expenses (
                                ExpenseId INT IDENTITY(1,1) PRIMARY KEY,
                                UserId INT NOT NULL,
                                CategoryId INT NOT NULL,
                                Amount DECIMAL(18,2) NOT NULL,
                                ExpenseDate DATE NOT NULL,
                                Description NVARCHAR(200) NULL,
                                IsRecurring BIT DEFAULT 0,
                                RecurringId INT NULL,
                                CreatedAt DATETIME DEFAULT GETDATE(),
                                FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE,
                                FOREIGN KEY (CategoryId) REFERENCES Categories(CategoryId)
                            )
                            CREATE TABLE SavingsGoals (
                                GoalId INT IDENTITY(1,1) PRIMARY KEY,
                                UserId INT NOT NULL,
                                GoalName NVARCHAR(100) NOT NULL,
                                TargetAmount DECIMAL(18,2) NOT NULL,
                                CurrentAmount DECIMAL(18,2) DEFAULT 0,
                                Deadline DATE NULL,
                                CreatedAt DATETIME DEFAULT GETDATE(),
                                FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE
                            )
                            CREATE TABLE RecurringExpenses (
                                RecurringId INT IDENTITY(1,1) PRIMARY KEY,
                                UserId INT NOT NULL,
                                CategoryId INT NOT NULL,
                                Amount DECIMAL(18,2) NOT NULL,
                                Frequency NVARCHAR(20) NOT NULL,
                                StartDate DATE NOT NULL,
                                LastProcessed DATE NULL,
                                IsActive BIT DEFAULT 1,
                                FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE,
                                FOREIGN KEY (CategoryId) REFERENCES Categories(CategoryId)
                            )
                            CREATE TABLE BudgetSettings (
                                SettingId INT IDENTITY(1,1) PRIMARY KEY,
                                UserId INT NOT NULL UNIQUE,
                                MonthlyIncome DECIMAL(18,2) DEFAULT 0,
                                DailyLimit DECIMAL(18,2) DEFAULT 0,
                                WeeklyLimit DECIMAL(18,2) DEFAULT 0,
                                Currency NVARCHAR(10) DEFAULT 'USD',
                                FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE
                            )
                        END
                    ";
                    using (var cmd = new SqlCommand(createSql, conn)) cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Database initialization failed: " + ex.Message);
            }
        }

        // ========== USER METHODS ==========
        public User GetUserByUsername(string username)
        {
            using (var conn = GetConnection())
            {
                string sql = "SELECT * FROM Users WHERE Username = @Username";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Username", username);
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new User
                            {
                                UserId = (int)reader["UserId"],
                                Username = reader["Username"].ToString(),
                                Email = reader["Email"].ToString(),
                                Password = reader["Password"].ToString(),
                                CreatedAt = (DateTime)reader["CreatedAt"]
                            };
                        }
                    }
                }
            }
            return null;
        }

        public User GetUserByEmail(string email)
        {
            using (var conn = GetConnection())
            {
                string sql = "SELECT * FROM Users WHERE Email = @Email";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Email", email);
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new User
                            {
                                UserId = (int)reader["UserId"],
                                Username = reader["Username"].ToString(),
                                Email = reader["Email"].ToString(),
                                Password = reader["Password"].ToString(),
                                CreatedAt = (DateTime)reader["CreatedAt"]
                            };
                        }
                    }
                }
            }
            return null;
        }

        public int CreateUser(User user)
        {
            using (var conn = GetConnection())
            {
                string sql = "INSERT INTO Users (Username, Email, Password) OUTPUT INSERTED.UserId VALUES (@Username, @Email, @Password)";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Username", user.Username);
                    cmd.Parameters.AddWithValue("@Email", user.Email);
                    cmd.Parameters.AddWithValue("@Password", user.Password);
                    conn.Open();
                    int userId = (int)cmd.ExecuteScalar();
                    CreateDefaultCategories(userId);
                    CreateBudgetSetting(new BudgetSetting { UserId = userId });
                    return userId;
                }
            }
        }

        // ========== CATEGORY METHODS ==========
        private void CreateDefaultCategories(int userId)
        {
            var categories = new[] { "Food", "Transport", "Entertainment", "Shopping", "Bills", "Other" };
            using (var conn = GetConnection())
            {
                conn.Open();
                foreach (var cat in categories)
                {
                    string sql = "INSERT INTO Categories (UserId, CategoryName) VALUES (@UserId, @CategoryName)";
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        cmd.Parameters.AddWithValue("@CategoryName", cat);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        public List<Category> GetCategories(int userId)
        {
            var categories = new List<Category>();
            using (var conn = GetConnection())
            {
                string sql = "SELECT * FROM Categories WHERE UserId = @UserId ORDER BY CategoryName";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            categories.Add(new Category
                            {
                                CategoryId = (int)reader["CategoryId"],
                                UserId = (int)reader["UserId"],
                                CategoryName = reader["CategoryName"].ToString()
                            });
                        }
                    }
                }
            }
            return categories;
        }

        // ========== EXPENSE METHODS ==========
        public void AddExpense(Expense expense)
        {
            using (var conn = GetConnection())
            {
                string sql = @"INSERT INTO Expenses (UserId, CategoryId, Amount, ExpenseDate, Description, IsRecurring, RecurringId) 
                              VALUES (@UserId, @CategoryId, @Amount, @ExpenseDate, @Description, @IsRecurring, @RecurringId)";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", expense.UserId);
                    cmd.Parameters.AddWithValue("@CategoryId", expense.CategoryId);
                    cmd.Parameters.AddWithValue("@Amount", expense.Amount);
                    cmd.Parameters.AddWithValue("@ExpenseDate", expense.ExpenseDate);
                    cmd.Parameters.AddWithValue("@Description", (object)expense.Description ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@IsRecurring", expense.IsRecurring);
                    cmd.Parameters.AddWithValue("@RecurringId", expense.RecurringId.HasValue ? (object)expense.RecurringId.Value : DBNull.Value);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public List<Expense> GetExpenses(int userId, int limit = 50)
        {
            var expenses = new List<Expense>();
            using (var conn = GetConnection())
            {
                string sql = @"SELECT TOP (@Limit) e.*, c.CategoryName 
                              FROM Expenses e
                              INNER JOIN Categories c ON e.CategoryId = c.CategoryId
                              WHERE e.UserId = @UserId
                              ORDER BY e.ExpenseDate DESC, e.CreatedAt DESC";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.Parameters.AddWithValue("@Limit", limit);
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            expenses.Add(new Expense
                            {
                                ExpenseId = (int)reader["ExpenseId"],
                                UserId = (int)reader["UserId"],
                                CategoryId = (int)reader["CategoryId"],
                                Amount = (decimal)reader["Amount"],
                                ExpenseDate = Convert.ToDateTime(reader["ExpenseDate"]).ToString("yyyy-MM-dd"),
                                Description = reader["Description"] != DBNull.Value ? reader["Description"].ToString() : "",
                                IsRecurring = (bool)reader["IsRecurring"],
                                RecurringId = reader["RecurringId"] != DBNull.Value ? (int?)reader["RecurringId"] : null,
                                CreatedAt = (DateTime)reader["CreatedAt"],
                                CategoryName = reader["CategoryName"].ToString()
                            });
                        }
                    }
                }
            }
            return expenses;
        }

        public void DeleteExpense(int expenseId)
        {
            using (var conn = GetConnection())
            {
                string sql = "DELETE FROM Expenses WHERE ExpenseId = @ExpenseId";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@ExpenseId", expenseId);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public decimal GetTodayTotal(int userId)
        {
            using (var conn = GetConnection())
            {
                string today = DateTime.Now.ToString("yyyy-MM-dd");
                string sql = "SELECT ISNULL(SUM(Amount), 0) FROM Expenses WHERE UserId = @UserId AND ExpenseDate = @Today";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.Parameters.AddWithValue("@Today", today);
                    conn.Open();
                    return (decimal)cmd.ExecuteScalar();
                }
            }
        }

        public decimal GetWeekTotal(int userId)
        {
            using (var conn = GetConnection())
            {
                DateTime today = DateTime.Now;
                int daysSinceMonday = (int)today.DayOfWeek - 1;
                if (daysSinceMonday < 0) daysSinceMonday = 6;
                DateTime startOfWeek = today.AddDays(-daysSinceMonday);
                string startDate = startOfWeek.ToString("yyyy-MM-dd");
                string sql = "SELECT ISNULL(SUM(Amount), 0) FROM Expenses WHERE UserId = @UserId AND ExpenseDate >= @StartDate";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.Parameters.AddWithValue("@StartDate", startDate);
                    conn.Open();
                    return (decimal)cmd.ExecuteScalar();
                }
            }
        }

        public decimal GetMonthTotal(int userId)
        {
            using (var conn = GetConnection())
            {
                string monthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).ToString("yyyy-MM-dd");
                string sql = "SELECT ISNULL(SUM(Amount), 0) FROM Expenses WHERE UserId = @UserId AND ExpenseDate >= @MonthStart";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.Parameters.AddWithValue("@MonthStart", monthStart);
                    conn.Open();
                    return (decimal)cmd.ExecuteScalar();
                }
            }
        }

        // ========== BUDGET METHODS ==========
        public BudgetSetting GetBudgetSetting(int userId)
        {
            using (var conn = GetConnection())
            {
                string sql = "SELECT * FROM BudgetSettings WHERE UserId = @UserId";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new BudgetSetting
                            {
                                SettingId = (int)reader["SettingId"],
                                UserId = (int)reader["UserId"],
                                MonthlyIncome = (decimal)reader["MonthlyIncome"],
                                DailyLimit = (decimal)reader["DailyLimit"],
                                WeeklyLimit = (decimal)reader["WeeklyLimit"],
                                Currency = reader["Currency"].ToString()
                            };
                        }
                    }
                }
            }
            return null;
        }

        public void CreateBudgetSetting(BudgetSetting budget)
        {
            using (var conn = GetConnection())
            {
                string sql = @"INSERT INTO BudgetSettings (UserId, MonthlyIncome, DailyLimit, WeeklyLimit, Currency) 
                              VALUES (@UserId, @MonthlyIncome, @DailyLimit, @WeeklyLimit, @Currency)";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", budget.UserId);
                    cmd.Parameters.AddWithValue("@MonthlyIncome", budget.MonthlyIncome);
                    cmd.Parameters.AddWithValue("@DailyLimit", budget.DailyLimit);
                    cmd.Parameters.AddWithValue("@WeeklyLimit", budget.WeeklyLimit);
                    cmd.Parameters.AddWithValue("@Currency", budget.Currency ?? "USD");
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void UpdateBudgetSetting(BudgetSetting budget)
        {
            using (var conn = GetConnection())
            {
                string sql = @"UPDATE BudgetSettings SET MonthlyIncome = @MonthlyIncome, DailyLimit = @DailyLimit, WeeklyLimit = @WeeklyLimit, Currency = @Currency WHERE UserId = @UserId";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", budget.UserId);
                    cmd.Parameters.AddWithValue("@MonthlyIncome", budget.MonthlyIncome);
                    cmd.Parameters.AddWithValue("@DailyLimit", budget.DailyLimit);
                    cmd.Parameters.AddWithValue("@WeeklyLimit", budget.WeeklyLimit);
                    cmd.Parameters.AddWithValue("@Currency", budget.Currency ?? "USD");
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // ========== SAVINGS GOAL METHODS ==========
        public void AddSavingsGoal(SavingsGoal goal)
        {
            using (var conn = GetConnection())
            {
                string sql = @"INSERT INTO SavingsGoals (UserId, GoalName, TargetAmount, CurrentAmount, Deadline) 
                              VALUES (@UserId, @GoalName, @TargetAmount, @CurrentAmount, @Deadline)";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", goal.UserId);
                    cmd.Parameters.AddWithValue("@GoalName", goal.GoalName);
                    cmd.Parameters.AddWithValue("@TargetAmount", goal.TargetAmount);
                    cmd.Parameters.AddWithValue("@CurrentAmount", goal.CurrentAmount);
                    cmd.Parameters.AddWithValue("@Deadline", string.IsNullOrEmpty(goal.Deadline) ? DBNull.Value : (object)goal.Deadline);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public List<SavingsGoal> GetSavingsGoals(int userId)
        {
            var goals = new List<SavingsGoal>();
            using (var conn = GetConnection())
            {
                string sql = "SELECT * FROM SavingsGoals WHERE UserId = @UserId ORDER BY CreatedAt DESC";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            goals.Add(new SavingsGoal
                            {
                                GoalId = (int)reader["GoalId"],
                                UserId = (int)reader["UserId"],
                                GoalName = reader["GoalName"].ToString(),
                                TargetAmount = (decimal)reader["TargetAmount"],
                                CurrentAmount = (decimal)reader["CurrentAmount"],
                                Deadline = reader["Deadline"] != DBNull.Value ? Convert.ToDateTime(reader["Deadline"]).ToString("yyyy-MM-dd") : "",
                                CreatedAt = (DateTime)reader["CreatedAt"]
                            });
                        }
                    }
                }
            }
            return goals;
        }

        public void DeleteSavingsGoal(int goalId)
        {
            using (var conn = GetConnection())
            {
                string sql = "DELETE FROM SavingsGoals WHERE GoalId = @GoalId";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@GoalId", goalId);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // ========== RECURRING EXPENSE METHODS ==========
        public void AddRecurringExpense(RecurringExpense recurring)
        {
            using (var conn = GetConnection())
            {
                string sql = @"INSERT INTO RecurringExpenses (UserId, CategoryId, Amount, Frequency, StartDate, LastProcessed, IsActive) 
                              VALUES (@UserId, @CategoryId, @Amount, @Frequency, @StartDate, @LastProcessed, @IsActive)";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", recurring.UserId);
                    cmd.Parameters.AddWithValue("@CategoryId", recurring.CategoryId);
                    cmd.Parameters.AddWithValue("@Amount", recurring.Amount);
                    cmd.Parameters.AddWithValue("@Frequency", recurring.Frequency);
                    cmd.Parameters.AddWithValue("@StartDate", recurring.StartDate);
                    cmd.Parameters.AddWithValue("@LastProcessed", string.IsNullOrEmpty(recurring.LastProcessed) ? DBNull.Value : (object)recurring.LastProcessed);
                    cmd.Parameters.AddWithValue("@IsActive", recurring.IsActive);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public List<RecurringExpense> GetRecurringExpenses(int userId)
        {
            var recurrings = new List<RecurringExpense>();
            using (var conn = GetConnection())
            {
                string sql = @"SELECT r.*, c.CategoryName FROM RecurringExpenses r INNER JOIN Categories c ON r.CategoryId = c.CategoryId WHERE r.UserId = @UserId AND r.IsActive = 1 ORDER BY r.StartDate DESC";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            recurrings.Add(new RecurringExpense
                            {
                                RecurringId = (int)reader["RecurringId"],
                                UserId = (int)reader["UserId"],
                                CategoryId = (int)reader["CategoryId"],
                                Amount = (decimal)reader["Amount"],
                                Frequency = reader["Frequency"].ToString(),
                                StartDate = Convert.ToDateTime(reader["StartDate"]).ToString("yyyy-MM-dd"),
                                LastProcessed = reader["LastProcessed"] != DBNull.Value ? Convert.ToDateTime(reader["LastProcessed"]).ToString("yyyy-MM-dd") : "",
                                IsActive = (bool)reader["IsActive"],
                                CategoryName = reader["CategoryName"].ToString()
                            });
                        }
                    }
                }
            }
            return recurrings;
        }

        public void DeleteRecurringExpense(int recurringId)
        {
            using (var conn = GetConnection())
            {
                string sql = "DELETE FROM RecurringExpenses WHERE RecurringId = @RecurringId";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@RecurringId", recurringId);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void UpdateRecurringLastProcessed(int recurringId, string date)
        {
            using (var conn = GetConnection())
            {
                string sql = "UPDATE RecurringExpenses SET LastProcessed = @LastProcessed WHERE RecurringId = @RecurringId";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@LastProcessed", date);
                    cmd.Parameters.AddWithValue("@RecurringId", recurringId);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // ========== RESET METHODS ==========
        public void ResetAllData(int userId)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        string deleteExpenses = "DELETE FROM Expenses WHERE UserId = @UserId";
                        using (var cmd = new SqlCommand(deleteExpenses, conn, transaction)) { cmd.Parameters.AddWithValue("@UserId", userId); cmd.ExecuteNonQuery(); }

                        string deleteGoals = "DELETE FROM SavingsGoals WHERE UserId = @UserId";
                        using (var cmd = new SqlCommand(deleteGoals, conn, transaction)) { cmd.Parameters.AddWithValue("@UserId", userId); cmd.ExecuteNonQuery(); }

                        string deleteRecurring = "DELETE FROM RecurringExpenses WHERE UserId = @UserId";
                        using (var cmd = new SqlCommand(deleteRecurring, conn, transaction)) { cmd.Parameters.AddWithValue("@UserId", userId); cmd.ExecuteNonQuery(); }

                        string resetBudget = @"UPDATE BudgetSettings SET MonthlyIncome = 0, DailyLimit = 0, WeeklyLimit = 0, Currency = 'USD' WHERE UserId = @UserId";
                        using (var cmd = new SqlCommand(resetBudget, conn, transaction)) { cmd.Parameters.AddWithValue("@UserId", userId); cmd.ExecuteNonQuery(); }

                        transaction.Commit();
                    }
                    catch { transaction.Rollback(); throw; }
                }
            }
        }
    }
}