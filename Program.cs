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

            bool labMode = args.Contains("--lab", StringComparer.OrdinalIgnoreCase);
            string role = GetArgValue(args, "--role") ?? "auto";
            string peer = GetArgValue(args, "--peer") ?? string.Empty;

            if (labMode)
            {
                GlobalFlags.ForceLabMode(); // <- en vez de setear IsLabMode directamente
                Console.WriteLine("[Boot] 🧪 Modo laboratorio activado por argumento (--lab).");
            }

            Console.WriteLine("──────────────────────────────────────────────────────────────");
            Console.WriteLine("✈️  SharedCockpitClient iniciado");
            Console.WriteLine($"[Boot] Versión: 1.0 | LabMode={GlobalFlags.IsLabMode} | Role={role} | Peer={peer}");
            Console.WriteLine("──────────────────────────────────────────────────────────────\n");

            var aircraftState = new AircraftStateManager();
            var sim = new SimConnectManager(aircraftState);
            var sync = new SyncController(sim, aircraftState);

            if (!string.IsNullOrWhiteSpace(role) && role != "auto")
                sim.SetUserRole(role.ToUpperInvariant());

            if (GlobalFlags.IsLabMode)
                sim.EnableMockMode();

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

        private static string? GetArgValue(string[] args, string key)
        {
            var index = Array.FindIndex(args, a => a.Equals(key, StringComparison.OrdinalIgnoreCase));
            if (index >= 0 && index + 1 < args.Length)
                return args[index + 1];
            return null;
        }
    }
}
