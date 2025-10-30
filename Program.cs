using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SharedCockpitClient.FlightData;
using SharedCockpitClient.Network;
using SharedCockpitClient.Persistence;
using SharedCockpitClient.Walkaround;

namespace SharedCockpitClient
{
    internal static class Program
    {
        static async Task Main(string[] args)
        {
            GlobalFlags.PeerAddress = Properties.Settings.Default["PeerAddress"]?.ToString() ?? string.Empty;

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
            Console.WriteLine($"[Boot] Versión: 5.0 | LabMode={GlobalFlags.IsLabMode} | Role={(isHost ? "host" : "client")} | Peer={peer}");
            Console.WriteLine("──────────────────────────────────────────────────────────────\n");

            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };

            var store = new SnapshotStore();
            var aircraftState = new AircraftStateManager();
            try
            {
                var restored = await store.LoadAsync(cts.Token).ConfigureAwait(false);
                if (restored.Count > 0)
                {
                    aircraftState.ApplySnapshot(new Dictionary<string, object?>(restored));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Boot] ⚠️ No se pudo cargar snapshot previo: {ex.Message}");
            }

            var simManager = new SimConnectManager(aircraftState);
            simManager.OnSnapshot += (snapshot, isDiff) =>
            {
                if (!isDiff)
                {
                    _ = store.SaveAsync(snapshot.ToFlatDictionary(), CancellationToken.None);
                }
            };
            simManager.Start();
            var labConsoleRunning = LabConsole.StartIfEnabledAndOffline(simManager);

            using var ws = new WebSocketManager(isHost, peerUri);
            await ws.StartAsync(cts.Token).ConfigureAwait(false);

            using var sync = new SyncController(ws, aircraftState, simManager);
            var walkaroundSync = new WalkaroundSync(ws, sync.LocalInstanceId);
            sync.AttachWalkaround(walkaroundSync);

            Console.WriteLine($"[Boot] LocalInstanceId = {sync.LocalInstanceId}");

            if (labConsoleRunning)
            {
                using var labLinkedCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, LabConsole.CancellationToken);
                RunLabLoop(sync, aircraftState, walkaroundSync, labLinkedCts.Token);

                if (!cts.Token.IsCancellationRequested && labLinkedCts.Token.IsCancellationRequested && !GlobalFlags.IsLabMode)
                {
                    await WaitForExitAsync(cts.Token).ConfigureAwait(false);
                }
            }
            else
            {
                Console.WriteLine("[Boot] ℹ️ Integración con MSFS real requerirá ejecutar junto al simulador.");
                await WaitForExitAsync(cts.Token).ConfigureAwait(false);
            }

            try
            {
                await store.SaveAsync(aircraftState.GetSnapshot(), CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Boot] ⚠️ Error guardando snapshot final: {ex.Message}");
            }

            Console.WriteLine("\n[Boot] 🚪 Aplicación finalizada correctamente.");
        }

        private static void RunLabLoop(SyncController sync, AircraftStateManager state, WalkaroundSync walkaroundSync, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (!LabConsole.IsRunning || !GlobalFlags.IsLabMode)
                    break;

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
                    case "set":
                        if (parts.Length >= 3)
                        {
                            var path = parts[1];
                            var raw = string.Join(' ', parts.Skip(2));
                            if (double.TryParse(raw, out var dbl))
                                state.Set(path, dbl);
                            else if (bool.TryParse(raw, out var b))
                                state.Set(path, b);
                            else
                                state.Set(path, raw);
                        }
                        else
                        {
                            Console.WriteLine("Uso: set <ruta> <valor>");
                        }
                        break;

                    case "toggle":
                        if (parts.Length >= 2)
                        {
                            var path = parts[1];
                            var current = state.Get(path);
                            if (current is bool boolValue)
                            {
                                state.Set(path, !boolValue);
                            }
                            else
                            {
                                state.Set(path, true);
                            }
                        }
                        else
                        {
                            Console.WriteLine("Uso: toggle <ruta>");
                        }
                        break;

                    case "pose":
                        if (parts.Length >= 5)
                        {
                            if (double.TryParse(parts[1], out var lat) &&
                                double.TryParse(parts[2], out var lon) &&
                                double.TryParse(parts[3], out var alt) &&
                                double.TryParse(parts[4], out var hdg))
                            {
                                var pose = new AvatarPose
                                {
                                    lat = lat,
                                    lon = lon,
                                    alt = alt,
                                    hdg = hdg,
                                    pitch = 0,
                                    bank = 0,
                                    state = "walk"
                                };
                                _ = walkaroundSync.PublishPoseAsync(pose, token);
                            }
                        }
                        else
                        {
                            Console.WriteLine("Uso: pose <lat> <lon> <alt> <hdg>");
                        }
                        break;

                    case "state":
                        var snapshot = state.GetSnapshot();
                        Console.WriteLine("📋 Estado actual del avión:");
                        foreach (var kv in snapshot.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
                            Console.WriteLine($"  {kv.Key} = {kv.Value}");
                        break;

                    default:
                        Console.WriteLine("Comando no reconocido.");
                        break;
                }
            }
        }

        private static async Task WaitForExitAsync(CancellationToken token)
        {
            try
            {
                await Task.Delay(Timeout.Infinite, token).ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
            }
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
