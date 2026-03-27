using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;

namespace Turfirm.Infrastructure
{
    public static class Db
    {
        private static bool _initialized;
        public static string DatabaseName { get; private set; }
        public static string InstanceName { get; private set; }
        public static string MasterConnection { get; private set; }
        public static string AppConnection { get; private set; }

        static Db()
        {
            Initialize();
        }

        public static void Initialize()
        {
            if (_initialized) return;

            DatabaseName = ReadSetting("DatabaseName", "DBTurfirma");

            var preferredInstance = ReadSetting("SqlInstance", string.Empty);
            var candidates = new List<string>();
            if (!string.IsNullOrWhiteSpace(preferredInstance))
                candidates.Add(preferredInstance.Trim());

            candidates.AddRange(new[]
            {
                @"(localdb)\MSSQLLocalDB",
                @"(localdb)\mssqllocaldb",
                @".\SQLEXPRESS",
                @".\SQLExpress",
                "."
            });

            Exception lastError = null;
            foreach (var instance in candidates.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                TryPrepareInstance(instance);
                var masterCs = BuildConnectionString(instance, "master");
                try
                {
                    using (var testConnection = new SqlConnection(masterCs))
                    {
                        testConnection.Open();
                    }

                    InstanceName = instance;
                    MasterConnection = masterCs;
                    AppConnection = BuildConnectionString(instance, DatabaseName);
                    _initialized = true;
                    return;
                }
                catch (Exception ex)
                {
                    lastError = ex;
                }
            }

            var tried = string.Join(", ", candidates.Distinct(StringComparer.OrdinalIgnoreCase));
            throw new InvalidOperationException(
                $"Не удалось подключиться к SQL Server. Проверьте установленный экземпляр SQL Server/LocalDB. " +
                $"Пробовали: {tried}. Вы можете явно указать экземпляр в App.config (ключ SqlInstance).",
                lastError);
        }

        public static SqlConnection Open(string cs)
        {
            Initialize();
            var connection = new SqlConnection(cs);
            connection.Open();
            return connection;
        }

        private static string BuildConnectionString(string instance, string database)
        {
            return $"Server={instance};Integrated Security=true;Initial Catalog={database};TrustServerCertificate=True;Connect Timeout=5";
        }

        private static void TryPrepareInstance(string instance)
        {
            if (!instance.StartsWith("(localdb)", StringComparison.OrdinalIgnoreCase))
                return;

            var name = ExtractLocalDbName(instance);
            if (string.IsNullOrWhiteSpace(name))
                return;

            // Не выбрасываем исключение здесь — это best-effort подготовка экземпляра.
            // Если не получится, обычная попытка подключения ниже даст понятную ошибку.
            if (RunTool("sqllocaldb", $"i {name}") != 0)
                RunTool("sqllocaldb", $"create {name}");

            RunTool("sqllocaldb", $"start {name}");
        }

        private static string ExtractLocalDbName(string instance)
        {
            var idx = instance.IndexOf('\\');
            return idx < 0 || idx == instance.Length - 1
                ? string.Empty
                : instance.Substring(idx + 1);
        }

        private static int RunTool(string fileName, string arguments)
        {
            try
            {
                using (var process = new Process())
                {
                    process.StartInfo = new ProcessStartInfo
                    {
                        FileName = fileName,
                        Arguments = arguments,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };

                    process.Start();
                    process.WaitForExit(3000);
                    if (!process.HasExited)
                    {
                        process.Kill();
                        return -1;
                    }

                    return process.ExitCode;
                }
            }
            catch
            {
                return -1;
            }
        }

        private static string ReadSetting(string key, string fallback)
        {
            var value = ConfigurationManager.AppSettings[key];
            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }
    }
}
