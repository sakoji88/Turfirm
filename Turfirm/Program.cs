using System;
using System.Threading;
using System.Windows.Forms;
using Turfirm.Infrastructure;
using Turfirm.Services;

namespace Turfirm
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Application.ThreadException += HandleThreadException;
            AppDomain.CurrentDomain.UnhandledException += HandleUnhandledException;

            try
            {
                Db.Initialize();
                DatabaseInitializer.EnsureCreatedAndSeeded();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Не удалось подключиться к SQL Server/LocalDB.\n" +
                    "Проверьте имя экземпляра в App.config (ключ SqlInstance).\n\n" +
                    $"Техническая информация: {ex.Message}",
                    "Ошибка подключения к БД",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }
            Application.Run(new LoginForm());
        }

        private static void HandleThreadException(object sender, ThreadExceptionEventArgs e)
        {
            MessageBox.Show($"Произошла ошибка: {e.Exception.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private static void HandleUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;
            MessageBox.Show($"Критическая ошибка: {ex?.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
