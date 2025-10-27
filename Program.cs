using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SharedCockpitClient.Network;
using SharedCockpitClient.FlightData;

namespace SharedCockpitClient
{
    internal static class Program
    {
        static async Task Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.Title = "SharedCockpitClient";

            bool labMode = args.Contains("--lab", StringComparer.OrdinalIgnoreCase);
            string role = GetArgValue(args, "--role") ?? "host";
            string peer = GetArgValue(args, "--peer") ?? string.Empty;

            if (labMode)
            {
                GlobalFlags.ForceLabMode();
                Console.WriteLine("[Boot] 🧪 Modo laboratorio activado por argumento (--lab).");
            }

            bool isHost = role.Equals("host", StringComparison.OrdinalIgnoreCase);
            Uri? peerUri = null;
            if (!isHost)
            {
                if (string.IsNullOrWhiteSpace(peer))
                {
                    Console.WriteLine("[Boot] ❌ Debes especificar --peer <host:puerto> en modo cliente.");
                    return;
                }

                peerUri = new Uri($"ws://{peer}");
            }

            Console.WriteLine("──────────────────────────────────────────────────────────────");
            Console.WriteLine("✈️  SharedCockpitClient iniciado");
            Console.WriteLine($"[Boot] Versión: 1.0 | LabMode={GlobalFlags.IsLabMode} | Role={(isHost ? "host" : "client")} | Peer={peer}");
            Console.WriteLine("──────────────────────────────────────────────────────────────\n");

            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };

            // 🔌 Inicializa la conexión de red
            using var ws = new WebSocketManager(isHost, peerUri);
            await ws.StartAsync(cts.Token).ConfigureAwait(false);

            // 🧠 Inicializa el estado del avión
            var aircraftState = new AircraftStateManager();

            // 🔄 Crea el controlador de sincronización con referencia al estado
            using var sync = new SyncController(ws, aircraftState);
            Console.WriteLine($"[Boot] LocalInstanceId = {sync.LocalInstanceId}");

            // 🧪 Loop de laboratorio (modo offline)
            if (GlobalFlags.IsLabMode)
            {
                RunLabLoop(sync, aircraftState, cts.Token);
            }
            else
            {
                Console.WriteLine("[Boot] ℹ️ Modo estándar aún no implementado para Fase 4. Usando loop de laboratorio.");
                RunLabLoop(sync, aircraftState, cts.Token);
            }

            Console.WriteLine("\n[Boot] 🚪 Aplicación finalizada correctamente.");
        }

        /// <summary>
        /// Loop interactivo de laboratorio.
        /// Permite probar flaps, tren y otras propiedades manualmente.
        /// </summary>
        private static void RunLabLoop(SyncController sync, AircraftStateManager state, CancellationToken token)
        {
            Console.WriteLine("Lab Mode activo. Comandos: flaps <num>, gear, lights <on/off>, engine <on/off>, door <open/close>, state, exit");

            while (!token.IsCancellationRequested)
            {
                Console.Write("> ");
                var line = Console.ReadLine();
                if (line is null)
                    break;

                var input = line.Trim();
                if (string.IsNullOrEmpty(input))
                    continue;

                if (string.Equals(input, "exit", StringComparison.OrdinalIgnoreCase))
                    break;

                var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var cmd = parts[0].ToLowerInvariant();

                switch (cmd)
                {
                    case "flaps":
                        if (parts.Length >= 2 && double.TryParse(parts[1], out var target))
                        {
                            _ = sync.SetFlapsLocalAsync(target);
                        }
                        else
                        {
                            Console.WriteLine("Uso: flaps <valor>");
                        }
                        break;

                    case "gear":
                        _ = sync.ToggleGearLocalAsync();
                        break;

                    case "lights":
                        if (parts.Length >= 2)
                        {
                            bool val = string.Equals(parts[1], "on", StringComparison.OrdinalIgnoreCase);
                            state.Set("Lights", val);
                        }
                        else
                        {
                            Console.WriteLine("Uso: lights on/off");
                        }
                        break;

                    case "engine":
                        if (parts.Length >= 2)
                        {
                            bool val = string.Equals(parts[1], "on", StringComparison.OrdinalIgnoreCase);
                            state.Set("EngineOn", val);
                        }
                        else
                        {
                            Console.WriteLine("Uso: engine on/off");
                        }
                        break;

                    case "door":
                        if (parts.Length >= 2)
                        {
                            bool val = string.Equals(parts[1], "open", StringComparison.OrdinalIgnoreCase);
                            state.Set("DoorOpen", val);
                        }
                        else
                        {
                            Console.WriteLine("Uso: door open/close");
                        }
                        break;

                    case "state":
                        var snapshot = state.GetSnapshot();
                        Console.WriteLine("📋 Estado actual del avión:");
                        foreach (var kv in snapshot)
                            Console.WriteLine($"  {kv.Key} = {kv.Value}");
                        break;

                    default:
                        Console.WriteLine("Comando no reconocido.");
                        break;
                }
            }
        }

        /// <summary>
        /// Devuelve el valor de un argumento CLI.
        /// </summary>
        private static string? GetArgValue(string[] args, string key)
        {
            var index = Array.FindIndex(args, a => a.Equals(key, StringComparison.OrdinalIgnoreCase));
            if (index >= 0 && index + 1 < args.Length)
                return args[index + 1];
            return null;
        }
    }
}
