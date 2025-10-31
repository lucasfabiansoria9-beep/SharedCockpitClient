#nullable enable
using System;
using System.IO;
using System.Windows.Forms;
using SharedCockpitClient.Session;
using SharedCockpitClient.Utils;

namespace SharedCockpitClient
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.Title = "SharedCockpitClient";

            var version = ReadVersionLabel();

            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Logger.Info("────────────────────────────────");
            Logger.Info("✈️ SharedCockpitClient iniciado");
            Logger.Info($"[Boot] Versión: {version}");
            Logger.Info("────────────────────────────────");

            StartupSessionInfo? sessionInfo = null;
            using (var dialog = new RoleDialog())
            {
                var result = dialog.ShowDialog();
                if (result != DialogResult.OK || dialog.StartupInfo == null)
                {
                    Logger.Info("[Boot] Cancelado por el usuario.");
                    return;
                }

                sessionInfo = dialog.StartupInfo;
            }

            AppSessionContext.Current = sessionInfo;

            using var mainForm = new MainForm(sessionInfo);
            Application.Run(mainForm);
        }

        private static string ReadVersionLabel()
        {
            try
            {
                var versionPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "version.txt");
                if (File.Exists(versionPath))
                {
                    var raw = File.ReadAllText(versionPath).Trim();
                    if (!string.IsNullOrEmpty(raw))
                        return raw;
                }
            }
            catch
            {
            }

            return "7.0";
        }
    }
}
