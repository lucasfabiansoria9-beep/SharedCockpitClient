using System;
using System.Linq;
using System.Threading.Tasks;
using SharedCockpitClient.FlightData;
using SharedCockpitClient.Network;

namespace SharedCockpitClient
{
    internal static class Program
    {
        static async Task Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.Title = "SharedCockpitClient";

            // ───────────────────────────────────────────────────────────────
            // 1️⃣ PARSEO DE ARGUMENTOS
            // ───────────────────────────────────────────────────────────────
            bool labMode = args.Contains("--lab", StringComparer.OrdinalIgnoreCase);
            string role = GetArgValue(args, "--role") ?? "auto";
            string peer = GetArgValue(args, "--peer") ?? string.Empty;

            if (labMode)
            {
                GlobalFlags.ForceLabMode();
                Console.WriteLine("[Boot] 🧪 Modo laboratorio activado por argumento (--lab).");
            }

            Console.WriteLine("──────────────────────────────────────────────────────────────");
            Console.WriteLine("✈️  SharedCockpitClient iniciado");
            Console.WriteLine($"[Boot] Versión: 1.0 | LabMode={GlobalFlags.IsLabMode} | Role={role} | Peer={peer}");
            Console.WriteLine("──────────────────────────────────────────────────────────────\n");

            // ───────────────────────────────────────────────────────────────
            // 2️⃣ INICIALIZAR COMPONENTES
            // ───────────────────────────────────────────────────────────────
            var aircraftState = new AircraftStateManager();
            var sim = new SimConnectManager(aircraftState);
            var sync = new SyncController(sim, aircraftState);

            // Establecer rol explícito si se pasa por argumento
            if (!string.IsNullOrWhiteSpace(role) && role != "auto")
                sim.SetUserRole(role.ToUpperInvariant());

            // Forzar modo laboratorio si está en pruebas
            if (GlobalFlags.IsLabMode)
                sim.EnableMockMode();

            // ───────────────────────────────────────────────────────────────
            // 3️⃣ ARRANQUE DEL CONTROLADOR DE SINCRONIZACIÓN
            // ───────────────────────────────────────────────────────────────
            try
            {
                await sync.RunAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Boot] ❌ Error al ejecutar SyncController: {ex.Message}");
            }
            finally
            {
                sync.Dispose();
                sim.Dispose();
            }

            Console.WriteLine("\n[Boot] 🚪 Aplicación finalizada correctamente.");
        }

        // ───────────────────────────────────────────────────────────────
        // FUNCIONES AUXILIARES
        // ───────────────────────────────────────────────────────────────
        private static string? GetArgValue(string[] args, string key)
        {
            var index = Array.FindIndex(args, a => a.Equals(key, StringComparison.OrdinalIgnoreCase));
            if (index >= 0 && index + 1 < args.Length)
                return args[index + 1];
            return null;
        }
    }
}
