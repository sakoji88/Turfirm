using System;
using System.Drawing;
using System.Windows.Forms;
using Turfirm.Services;

namespace Turfirm
{
    public class LoginForm : Form
    {
        private readonly AuthService _authService = new AuthService();
        private TextBox txtLogin;
        private TextBox txtPassword;

        public LoginForm()
        {
            Text = "Turfirm — Вход";
            Icon = SystemIcons.Information;
            Width = 520;
            Height = 360;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(245, 248, 255);

            var logo = new Label { Text = "🧳 Turfirm", Font = new Font("Segoe UI", 20, FontStyle.Bold), AutoSize = true, Top = 20, Left = 170 };
            Controls.Add(logo);

            Controls.Add(new Label { Text = "Email или телефон", Left = 80, Top = 100, Width = 150 });
            txtLogin = new TextBox { Left = 80, Top = 125, Width = 340 };
            Controls.Add(txtLogin);

            Controls.Add(new Label { Text = "Пароль", Left = 80, Top = 160, Width = 150 });
            txtPassword = new TextBox { Left = 80, Top = 185, Width = 340, PasswordChar = '*' };
            Controls.Add(txtPassword);

            var btnLogin = new Button { Text = "Войти", Left = 80, Top = 230, Width = 160, BackColor = Color.SteelBlue, ForeColor = Color.White };
            btnLogin.Click += BtnLogin_Click;
            Controls.Add(btnLogin);

            var btnRegister = new Button { Text = "Регистрация", Left = 260, Top = 230, Width = 160 };
            btnRegister.Click += (s, e) =>
            {
                using (var register = new RegisterForm())
                    register.ShowDialog(this);
            };
            Controls.Add(btnRegister);

            var hint = new Label
            {
                Left = 80,
                Top = 275,
                Width = 360,
                ForeColor = Color.DimGray,
                Text = "Тестовые аккаунты: admin@turfirma.local / manager@... / user@..."
            };
            Controls.Add(hint);
        }

        private void BtnLogin_Click(object sender, EventArgs e)
        {
            var session = _authService.Login(txtLogin.Text, txtPassword.Text);
            if (session == null)
            {
                MessageBox.Show("Неверный логин или пароль.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Hide();
            using (var main = new MainForm(session))
                main.ShowDialog();
            Show();
        }
    }
}
