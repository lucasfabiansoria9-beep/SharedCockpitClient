using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace SharedCockpitClient
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.Title = "SharedCockpitClient";

            var versionLabel = "6.8";
            try
            {
                var versionPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "version.txt");
                if (File.Exists(versionPath))
                {
                    var text = File.ReadAllText(versionPath).Trim();
                    if (!string.IsNullOrEmpty(text))
                    {
                        versionLabel = text;
                    }
                }
            }
            catch
            {
                // si no se puede leer la versión usamos el valor por defecto
            }
            var args = Environment.GetCommandLineArgs().Skip(1).ToArray();
            if (args.Any(a => string.Equals(a, "--lab", StringComparison.OrdinalIgnoreCase)))
            {
                GlobalFlags.ForceLabMode();
                Console.WriteLine("[Boot] 🧪 Modo laboratorio activado por argumento (--lab).");
            }

            GlobalFlags.Role = NormalizeRole(GetArgValue(args, "--role")) ?? GlobalFlags.Role;
            GlobalFlags.PeerAddress = GetArgValue(args, "--peer") ?? string.Empty;
            GlobalFlags.RoomName = GetArgValue(args, "--room") ?? GlobalFlags.RoomName;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Console.WriteLine("✈️ SharedCockpitClient iniciado");
            Console.WriteLine($"[Boot] Versión: {versionLabel} | RoleDialog abierto");

            using (var dialog = new RoleDialog())
            {
                var result = dialog.ShowDialog();
                if (result != DialogResult.OK)
                {
                    return;
                }
            }

            Application.Run(new MainForm());
        }

        private static string? NormalizeRole(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return value.Trim().Equals("host", StringComparison.OrdinalIgnoreCase)
                ? "host"
                : value.Trim().Equals("client", StringComparison.OrdinalIgnoreCase)
                    ? "client"
                    : null;
        }

        private static string? GetArgValue(string[] args, string key)
        {
            for (var i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], key, StringComparison.OrdinalIgnoreCase))
                {
                    return i + 1 < args.Length ? args[i + 1] : null;
                }
            }

            return null;
        }
    }
}
