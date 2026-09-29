using System;
using System.Drawing;
using System.Windows.Forms;

namespace Diplomski_Lopta
{
    public partial class LoginForm : Form
    {
        public LoginForm()
        {
            InitializeComponent();
            BuildUI();
        }

        private void BuildUI()
        {
            this.Size = new Size(400, 480);
            this.BackColor = Color.FromArgb(210, 205, 195);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            Label lblTitle = new Label
            {
                Text = "NBA/ABA/EUROLEAGUE\nProfessional basketball",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 110
            };

            Label lblUser = new Label
            {
                Text = "Korisničko ime:",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.Black,
                Location = new Point(55, 125),
                AutoSize = true
            };

            TextBox txtUser = new TextBox
            {
                Width = 280,
                Font = new Font("Segoe UI", 11),
                BackColor = Color.FromArgb(28, 27, 25),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Location = new Point(55, 148),
                Text = "admin"
            };

            Label lblPass = new Label
            {
                Text = "Lozinka:",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.Black,
                Location = new Point(55, 195),
                AutoSize = true
            };

            TextBox txtPass = new TextBox
            {
                Width = 242,
                Font = new Font("Segoe UI", 11),
                UseSystemPasswordChar = true,
                BackColor = Color.FromArgb(28, 27, 25),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Location = new Point(55, 218),
                Text = "nba2026"
            };

            Button btnTogglePass = new Button
            {
                Text = "👁",
                Width = 35,
                Height = 27,
                Location = new Point(300, 218),
                BackColor = Color.FromArgb(28, 27, 25),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Emoji", 10),
                Cursor = Cursors.Hand
            };
            btnTogglePass.FlatAppearance.BorderSize = 0;

            btnTogglePass.Click += (s, e) =>
            {
                txtPass.UseSystemPasswordChar = !txtPass.UseSystemPasswordChar;
                btnTogglePass.Text = txtPass.UseSystemPasswordChar ? "👁" : "🙈";
            };

            Button btnLogin = new Button
            {
                Text = "PRIJAVI SE",
                Width = 280,
                Height = 45,
                Location = new Point(55, 290),
                BackColor = Color.FromArgb(201, 8, 42),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnLogin.FlatAppearance.BorderSize = 0;

            btnLogin.Click += (s, e) =>
            {
                if (txtUser.Text == "admin" && txtPass.Text == "nba2026")
                {
                    this.Hide();
                    MainForm main = new MainForm();
                    main.ShowDialog();
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Netačno korisničko ime ili lozinka!", "Pokušajte ponovo",
                                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            this.Controls.Add(lblTitle);
            this.Controls.Add(lblUser);
            this.Controls.Add(txtUser);
            this.Controls.Add(lblPass);
            this.Controls.Add(txtPass);
            this.Controls.Add(btnTogglePass); 
            this.Controls.Add(btnLogin);
        }
    }
}