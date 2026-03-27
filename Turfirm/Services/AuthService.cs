using System;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using Turfirm.Infrastructure;

namespace Turfirm.Services
{
    public class AuthService
    {
        public CurrentSession Login(string login, string password)
        {
            var normalizedLogin = (login ?? string.Empty).Trim();
            var normalizedPassword = (password ?? string.Empty).Trim();

            using (var connection = Db.Open(Db.AppConnection))
            using (var command = new SqlCommand(@"
SELECT TOP 1 Id, FullName, Email, Phone, Role
FROM Users
WHERE (Email = @login OR Phone = @login)
  AND PasswordHash = CONVERT(NVARCHAR(256),HASHBYTES('SHA2_256',@password),2)", connection))
            {
                command.Parameters.AddWithValue("@login", normalizedLogin);
                command.Parameters.AddWithValue("@password", normalizedPassword);

                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read()) return null;
                    return new CurrentSession
                    {
                        UserId = reader.GetInt32(0),
                        FullName = reader.GetString(1),
                        Email = reader.GetString(2),
                        Phone = reader.GetString(3),
                        Role = (UserRole)reader.GetInt32(4)
                    };
                }
            }
        }

        public void Register(string fullName, string email, string phone, string password, string passportSeries, string passportNumber, DateTime passportIssue)
        {
            Validate(fullName, email, phone, password, passportSeries, passportNumber);
            using (var connection = Db.Open(Db.AppConnection))
            using (var command = new SqlCommand(@"
INSERT INTO Users(FullName,Email,Phone,PasswordHash,PassportSeries,PassportNumber,PassportIssueDate,Role)
VALUES(@fullName,@email,@phone,CONVERT(NVARCHAR(256),HASHBYTES('SHA2_256',@password),2),@series,@number,@issueDate,1)", connection))
            {
                command.Parameters.AddWithValue("@fullName", fullName.Trim());
                command.Parameters.AddWithValue("@email", email.Trim().ToLowerInvariant());
                command.Parameters.AddWithValue("@phone", phone.Trim());
                command.Parameters.AddWithValue("@password", password);
                command.Parameters.AddWithValue("@series", passportSeries.Trim());
                command.Parameters.AddWithValue("@number", passportNumber.Trim());
                command.Parameters.AddWithValue("@issueDate", passportIssue.Date);
                command.ExecuteNonQuery();
            }
        }

        public void UpdateProfile(CurrentSession session, string fullName, string email, string phone)
        {
            ValidateBase(fullName, email, phone);
            using (var connection = Db.Open(Db.AppConnection))
            using (var command = new SqlCommand("UPDATE Users SET FullName=@n, Email=@e, Phone=@p WHERE Id=@id", connection))
            {
                command.Parameters.AddWithValue("@id", session.UserId);
                command.Parameters.AddWithValue("@n", fullName.Trim());
                command.Parameters.AddWithValue("@e", email.Trim().ToLowerInvariant());
                command.Parameters.AddWithValue("@p", phone.Trim());
                command.ExecuteNonQuery();
            }
            session.FullName = fullName.Trim();
            session.Email = email.Trim().ToLowerInvariant();
            session.Phone = phone.Trim();
        }

        private static void Validate(string fullName, string email, string phone, string password, string passportSeries, string passportNumber)
        {
            ValidateBase(fullName, email, phone);
            if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
                throw new InvalidOperationException("Пароль должен содержать минимум 6 символов.");
            if (string.IsNullOrWhiteSpace(passportSeries) || string.IsNullOrWhiteSpace(passportNumber))
                throw new InvalidOperationException("Паспортные данные обязательны.");
        }

        private static void ValidateBase(string fullName, string email, string phone)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                throw new InvalidOperationException("Поле ФИО обязательно.");
            if (!Regex.IsMatch(email ?? string.Empty, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                throw new InvalidOperationException("Некорректный формат email.");
            if (string.IsNullOrWhiteSpace(phone) || phone.Trim().Length < 10)
                throw new InvalidOperationException("Некорректный номер телефона.");
        }
    }
}
