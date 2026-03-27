using System;
using System.Drawing;
using System.Windows.Forms;
using Turfirm.Services;

namespace Turfirm
{
    public class RegisterForm : Form
    {
        private readonly AuthService _auth = new AuthService();
        private TextBox txtFullName;
        private TextBox txtEmail;
        private TextBox txtPhone;
        private TextBox txtPassword;
        private TextBox txtPassportSeries;
        private TextBox txtPassportNumber;
        private DateTimePicker dtPassport;

        public RegisterForm()
        {
            Text = "Регистрация";
            Icon = SystemIcons.Question;
            Width = 520;
            Height = 500;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.White;

            int y = 20;
            txtFullName = AddField("ФИО", ref y);
            txtEmail = AddField("Email", ref y);
            txtPhone = AddField("Телефон", ref y);
            txtPassword = AddField("Пароль", ref y, true);
            txtPassportSeries = AddField("Серия паспорта", ref y);
            txtPassportNumber = AddField("Номер паспорта", ref y);
            Controls.Add(new Label { Text = "Дата выдачи", Left = 40, Top = y + 5, Width = 130 });
            dtPassport = new DateTimePicker { Left = 190, Top = y, Width = 250 };
            Controls.Add(dtPassport);
            y += 45;

            var btn = new Button { Text = "Создать аккаунт", Left = 190, Top = y + 10, Width = 160, BackColor = Color.SeaGreen, ForeColor = Color.White };
            btn.Click += Btn_Click;
            Controls.Add(btn);
        }

        private TextBox AddField(string label, ref int y, bool isPassword = false)
        {
            Controls.Add(new Label { Text = label, Left = 40, Top = y + 5, Width = 130 });
            var tb = new TextBox { Left = 190, Top = y, Width = 250, PasswordChar = isPassword ? '*' : '\0' };
            Controls.Add(tb);
            y += 45;
            return tb;
        }

        private void Btn_Click(object sender, EventArgs e)
        {
            try
            {
                _auth.Register(txtFullName.Text, txtEmail.Text, txtPhone.Text, txtPassword.Text, txtPassportSeries.Text, txtPassportNumber.Text, dtPassport.Value);
                MessageBox.Show("Регистрация успешна. Теперь войдите в систему.", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка регистрации", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
