using System;
using System.Windows.Forms;
using StudentBudgetTracker.DAL;
using StudentBudgetTracker.Helpers;
using StudentBudgetTracker.Models;

namespace StudentBudgetTracker
{
    public partial class LoginPage : Form
    {
        private DatabaseHelper _db = new DatabaseHelper();
        private HomePage _homePage;

        public LoginPage()
        {
            InitializeComponent();
            try { _db.InitializeDatabase(); }
            catch (Exception ex) { MessageBox.Show($"Database error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            WireUpEvents();
        }

        private void WireUpEvents()
        {
            button1.Click += new EventHandler(button1_Click);
            button3.Click += new EventHandler(button3_Click);
            linkLabel1.LinkClicked += new LinkLabelLinkClickedEventHandler(linkLabel1_LinkClicked);
            this.FormClosing += LoginPage_FormClosing;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                string username = textBox1.Text.Trim();
                string password = textBox2.Text.Trim();

                if (string.IsNullOrEmpty(username))
                {
                    MessageBox.Show("Please enter your username.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (string.IsNullOrEmpty(password))
                {
                    MessageBox.Show("Please enter your password.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var user = _db.GetUserByUsername(username);

                if (user == null)
                {
                    MessageBox.Show("❌ Username not found. Please sign up first.", "Login Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    textBox1.Clear();
                    textBox2.Clear();
                    textBox1.Focus();
                    return;
                }

                if (user.Password != password)
                {
                    MessageBox.Show("❌ Invalid password. Please try again.", "Login Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    textBox2.Clear();
                    textBox2.Focus();
                    return;
                }

                MessageBox.Show($"Welcome back, {user.Username}!", "Login Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Create a clean User object for session
                var sessionUser = new User
                {
                    UserId = user.UserId,
                    Username = user.Username,
                    Email = user.Email
                };
                SessionManager.CurrentUser = sessionUser;

                if (_homePage != null && !_homePage.IsDisposed) { _homePage.Close(); _homePage.Dispose(); }
                _homePage = new HomePage();
                _homePage.FormClosed += (s, args) => this.Close();
                _homePage.Show();
                this.Hide();
            }
            catch (Exception ex) { MessageBox.Show($"Login error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void LoginPage_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_homePage == null || _homePage.IsDisposed)
            {
                foreach (Form f in Application.OpenForms) { if (f is Form1) { f.Show(); return; } }
            }
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) { new SignupPage().Show(); this.Hide(); }
        private void button3_Click(object sender, EventArgs e) { new Form1().Show(); this.Close(); }
    }
}