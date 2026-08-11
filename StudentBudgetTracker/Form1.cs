using System;
using System.Windows.Forms;

namespace StudentBudgetTracker
{
    public partial class Form1 : Form
    {
        private LoginPage _loginPage;
        private SignupPage _signupPage;

        public Form1()
        {
            InitializeComponent();
            WireUpEvents();
        }

        private void WireUpEvents()
        {
            button1.Click += new EventHandler(button1_Click);
            //linkLabel1.LinkClicked += new LinkLabelLinkClickedEventHandler(linkLabel1_LinkClicked);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (_loginPage != null && !_loginPage.IsDisposed)
            {
                _loginPage.Close();
                _loginPage.Dispose();
            }
            _loginPage = new LoginPage();
            _loginPage.FormClosed += (s, args) => this.Show();
            _loginPage.Show();
            this.Hide();
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (_signupPage != null && !_signupPage.IsDisposed)
            {
                _signupPage.Close();
                _signupPage.Dispose();
            }
            _signupPage = new SignupPage();
            _signupPage.FormClosed += (s, args) => this.Show();
            _signupPage.Show();
            this.Hide();
        }

        private void Form1_Load(object sender, EventArgs e) { }
        private void label4_Click_1(object sender, EventArgs e) { }
        private void panel4_Paint(object sender, PaintEventArgs e) { }
        private void label1_Click(object sender, EventArgs e) { }
        private void label2_Click(object sender, EventArgs e) { }
        private void label4_Click(object sender, EventArgs e) { }
        private void richTextBox1_TextChanged(object sender, EventArgs e) { }
        private void label6_Click(object sender, EventArgs e) { }
        private void textBox1_TextChanged(object sender, EventArgs e) { }
    }
}