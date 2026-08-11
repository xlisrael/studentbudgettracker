using System;
using System.Windows.Forms;
using StudentBudgetTracker.DAL;
using StudentBudgetTracker.Helpers;
using StudentBudgetTracker.Models;

namespace StudentBudgetTracker
{
    public partial class SignupPage : Form
    {
        private DatabaseHelper _db = new DatabaseHelper();
        private HomePage _homePage;

        public SignupPage()
        {
            InitializeComponent();
            WireUpEvents();
        }

        private void WireUpEvents()
        {
            button1.Click += new EventHandler(button1_Click);
            button3.Click += new EventHandler(button3_Click);
            linkLabel1.LinkClicked += new LinkLabelLinkClickedEventHandler(linkLabel1_LinkClicked);
            this.FormClosing += SignupPage_FormClosing;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                string username = this.username.Text.Trim();
                string email = this.email.Text.Trim();
                string password = this.password.Text.Trim();

                // Validate inputs
                if (string.IsNullOrEmpty(username))
                {
                    MessageBox.Show("Please enter a username.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    this.username.Focus();
                    return;
                }
                if (string.IsNullOrEmpty(email))
                {
                    MessageBox.Show("Please enter your email.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    this.email.Focus();
                    return;
                }
                if (string.IsNullOrEmpty(password))
                {
                    MessageBox.Show("Please enter a password.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    this.password.Focus();
                    return;
                }

                // Check if username already exists
                var existingUser = _db.GetUserByUsername(username);
                if (existingUser != null)
                {
                    MessageBox.Show("❌ Username already taken. Please choose another.", "Signup Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    this.username.Clear();
                    this.username.Focus();
                    return;
                }

                // Check if email already exists
                var existingEmail = _db.GetUserByEmail(email);
                if (existingEmail != null)
                {
                    MessageBox.Show("❌ Email already registered. Please Login.", "Signup Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    this.email.Clear();
                    this.email.Focus();
                    return;
                }

                // Create new user
                var user = new User
                {
                    Username = username,
                    Email = email,
                    Password = password
                };

                int userId = _db.CreateUser(user);
                user.UserId = userId;
                SessionManager.CurrentUser = user;

                MessageBox.Show("✅ Account created successfully! Welcome!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                if (_homePage != null && !_homePage.IsDisposed) { _homePage.Close(); _homePage.Dispose(); }
                _homePage = new HomePage();
                _homePage.FormClosed += (s, args) => this.Close();
                _homePage.Show();
                this.Hide();
            }
            catch (Exception ex) { MessageBox.Show($"Signup error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void SignupPage_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_homePage == null || _homePage.IsDisposed)
            {
                foreach (Form f in Application.OpenForms) { if (f is Form1) { f.Show(); return; } }
            }
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) { new LoginPage().Show(); this.Hide(); }
        private void button3_Click(object sender, EventArgs e) { new Form1().Show(); this.Close(); }
    }
}