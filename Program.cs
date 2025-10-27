using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

            using var ws = new WebSocketManager(isHost, peerUri);
            await ws.StartAsync(cts.Token).ConfigureAwait(false);

            using var sync = new SyncController(ws);
            Console.WriteLine($"LocalInstanceId = {sync.LocalInstanceId}");

            if (GlobalFlags.IsLabMode)
            {
                RunLabLoop(sync, cts.Token);
            }
            else
            {
                Console.WriteLine("[Boot] ℹ️ Modo estándar aún no implementado para Fase 3. Usando loop de laboratorio.");
                RunLabLoop(sync, cts.Token);
            }

            Console.WriteLine("\n[Boot] 🚪 Aplicación finalizada correctamente.");
        }

        private static void RunLabLoop(SyncController sync, CancellationToken token)
        {
            Console.WriteLine("Lab Mode activo. Comandos: flaps <num>, gear, exit");
            while (!token.IsCancellationRequested)
            {
                Console.Write("> ");
                var line = Console.ReadLine();
                if (line is null)
                    break;

                var input = line.Trim();
                if (string.IsNullOrEmpty(input))
                    continue;

                if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
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
                    default:
                        Console.WriteLine("Comando no reconocido.");
                        break;
                }
            }
        }

        private static string? GetArgValue(string[] args, string key)
        {
            var index = Array.FindIndex(args, a => a.Equals(key, StringComparer.OrdinalIgnoreCase));
            if (index >= 0 && index + 1 < args.Length)
                return args[index + 1];
            return null;
        }
    }
}
