using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SharedCockpitClient.FlightData;
using SharedCockpitClient.Utils;

namespace SharedCockpitClient.Network
{
    public sealed class SyncController : IDisposable
    {
        private readonly SimConnectManager sim;
        private readonly AircraftStateManager aircraftState;

        private bool isHost = true;
        private string webSocketUrl = string.Empty;

        private WebSocketManager? ws;
        private WebSocketHost? hostServer;

        private CancellationTokenSource? loopToken;
        private System.Threading.Timer? syncTimer;

        private readonly object snapshotLock = new();
        private SimStateSnapshot? pendingSnapshot;
        private bool hasPendingSnapshot;

        private readonly string sourceTag = Guid.NewGuid().ToString();
        private readonly JsonSerializerOptions jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        // Anti-eco general (sincrónico inmediato)
        private int _suppressOutbound;

        // Anti-eco por variable: suprime emisiones de una variable hasta este instante (para cubrir animaciones async)
        private readonly ConcurrentDictionary<string, DateTime> _suppressUntilByVar = new(StringComparer.OrdinalIgnoreCase);

        // Para filtrar ruido: recordar último valor emitido por variable y aplicar EPS
        private readonly ConcurrentDictionary<string, double> _lastSentNumeric = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, bool> _lastSentBool = new(StringComparer.OrdinalIgnoreCase);

        private const double EPS_NUM = 0.02; // umbral de cambio mínimo para emitir valores numéricos

        public SyncController(SimConnectManager sim, AircraftStateManager aircraftState)
        {
            this.sim = sim ?? throw new ArgumentNullException(nameof(sim));
            this.aircraftState = aircraftState ?? throw new ArgumentNullException(nameof(aircraftState));

            this.sim.OnSnapshot += OnLocalSnapshot;
            this.aircraftState.OnStateChanged += OnLocalStateChanged;
        }

        public async Task RunAsync()
        {
            Console.OutputEncoding = Encoding.UTF8;

            ConfigureMode();
            SetupWebSocket();
            SetupShutdownHandlers();

            try
            {
                var msfsRunning = System.Diagnostics.Process.GetProcessesByName("FlightSimulator").Any();
                if (!msfsRunning) sim.EnableMockMode();
            }
            catch { sim.EnableMockMode(); }

            sim.Initialize(IntPtr.Zero);
            StartSynchronizationLoop();

            Logger.Info("Modo manual: comandos -> throttle 0.8 | flaps 15 | gear | brake | lights | door | avionics | exit");

            using var cts = new CancellationTokenSource();
            loopToken = cts;

            _ = Task.Run(async () =>
            {
                try
                {
                    while (!cts.IsCancellationRequested)
                    {
                        sim.ReceiveMessage();
                        await Task.Delay(100, cts.Token);
                    }
                }
                catch (TaskCanceledException) { }
            });

            RunManualTestLoop();
        }

        private void RunManualTestLoop()
        {
            while (true)
            {
                Console.Write("> ");
                var input = Console.ReadLine()?.Trim();
                if (string.IsNullOrEmpty(input)) continue;
                if (input.Equals("exit", StringComparison.OrdinalIgnoreCase)) break;

                var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var cmd = parts[0].ToLowerInvariant();
                double numValue;
                bool boolValue;

                try
                {
                    switch (cmd)
                    {
                        case "throttle":
                            if (parts.Length > 1 && double.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out numValue))
                                aircraftState.ApplyRemoteChange(nameof(AircraftStateManager.Throttle),
                                    JsonDocument.Parse(numValue.ToString(System.Globalization.CultureInfo.InvariantCulture)).RootElement);
                            else
                                Logger.Warn("Uso: throttle <0..1>");
                            break;

                        case "flaps":
                            if (parts.Length > 1 && double.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out numValue))
                                aircraftState.ApplyRemoteChange(nameof(AircraftStateManager.Flaps),
                                    JsonDocument.Parse(numValue.ToString(System.Globalization.CultureInfo.InvariantCulture)).RootElement);
                            else
                                Logger.Warn("Uso: flaps <grados>");
                            break;

                        case "gear":
                            boolValue = !aircraftState.GearDown;
                            aircraftState.ApplyRemoteChange(nameof(AircraftStateManager.GearDown),
                                JsonDocument.Parse(boolValue ? "true" : "false").RootElement);
                            break;

                        case "brake":
                            boolValue = !aircraftState.ParkingBrake;
                            aircraftState.ApplyRemoteChange(nameof(AircraftStateManager.ParkingBrake),
                                JsonDocument.Parse(boolValue ? "true" : "false").RootElement);
                            break;

                        case "lights":
                            boolValue = !aircraftState.LightsOn;
                            aircraftState.ApplyRemoteChange(nameof(AircraftStateManager.LightsOn),
                                JsonDocument.Parse(boolValue ? "true" : "false").RootElement);
                            break;

                        case "door":
                            boolValue = !aircraftState.DoorOpen;
                            aircraftState.ApplyRemoteChange(nameof(AircraftStateManager.DoorOpen),
                                JsonDocument.Parse(boolValue ? "true" : "false").RootElement);
                            break;

                        case "avionics":
                            boolValue = !aircraftState.AvionicsOn;
                            aircraftState.ApplyRemoteChange(nameof(AircraftStateManager.AvionicsOn),
                                JsonDocument.Parse(boolValue ? "true" : "false").RootElement);
                            break;

                        default:
                            Logger.Warn("Comando inválido. Usa: throttle 0.8 | flaps 15 | gear | brake | lights | door | avionics | exit");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warn($"⚠️ Error ejecutando comando: {ex.Message}");
                }
            }
        }

        private void ConfigureMode()
        {
            Console.WriteLine("Selecciona modo: 1) Host  2) Cliente");
            Console.Write("> ");
            var opt = Console.ReadLine();
            isHost = opt?.Trim() != "2";

            if (isHost)
            {
                var localIp = GetLocalIpAddress();
                Console.Write("Nombre de cabina: ");
                var name = Console.ReadLine()?.Trim();
                if (string.IsNullOrWhiteSpace(name)) name = "Cabina";

                Console.Write("¿Cabina pública? (S/N): ");
                bool pub = (Console.ReadLine()?.Trim().ToUpperInvariant() == "S");

                Console.WriteLine($"🚀 Cabina '{name}' iniciada en ws://{localIp}:8081 ({(pub ? "pública" : "privada")})");

                hostServer = new WebSocketHost(8081);
                hostServer.OnClientConnected += id => Logger.Info($"✅ Cliente conectado ({id})");
                hostServer.OnMessage += OnWebSocketMessage;
                hostServer.Start();
            }
            else
            {
                Console.WriteLine("👂 Escuchando broadcasts LAN (simplificado)...");
                var ip = "192.168.58.117";
                webSocketUrl = $"ws://{ip}:8081";
            }
        }

        private static string GetLocalIpAddress()
        {
            var ip = Dns.GetHostAddresses(Dns.GetHostName())
                .FirstOrDefault(i => i.AddressFamily == AddressFamily.InterNetwork);
            return ip?.ToString() ?? "127.0.0.1";
        }

        private void SetupWebSocket()
        {
            if (isHost) return;

            ws = new WebSocketManager(webSocketUrl, sim);
            ws.OnOpen += () => Logger.Info($"🌐 Conectado al servidor WebSocket: {webSocketUrl}");
            ws.OnMessage += OnWebSocketMessage;
            ws.OnError += msg => Logger.Warn($"Error WS: {msg}");
            ws.OnClose += () => Logger.Warn("Conexión WS cerrada.");
            ws.Connect();
        }

        private void SetupShutdownHandlers()
        {
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                Shutdown();
                Environment.Exit(0);
            };
        }

        private void OnWebSocketMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;
            ApplyRemotePayload(message);
        }

        private void ApplyRemotePayload(string message)
        {
            try
            {
                using var doc = JsonDocument.Parse(message);
                var root = doc.RootElement;

                if (!root.TryGetProperty("type", out var typeProp)) return;
                var type = typeProp.GetString();

                string? remoteSource = null;
                if (root.TryGetProperty("source", out var srcProp))
                    remoteSource = srcProp.GetString();

                if (!string.IsNullOrWhiteSpace(remoteSource) &&
                    string.Equals(remoteSource, sourceTag, StringComparison.Ordinal))
                {
                    return; // mensaje originado por nosotros
                }

                if (type == "aircraft_state" &&
                    root.TryGetProperty("variable", out var v) &&
                    root.TryGetProperty("value", out var val))
                {
                    var variable = v.GetString();
                    if (!string.IsNullOrEmpty(variable))
                    {
                        // Supresión por variable (1s) para cubrir animaciones async
                        _suppressUntilByVar[variable] = DateTime.UtcNow.AddMilliseconds(1000);

                        WithOutboundSuppressed(() =>
                        {
                            aircraftState.ApplyRemoteChange(variable, val);
                            Logger.Info($"[RemoteChange] {variable} = {FormatValue(val)} (desde {remoteSource ?? "desconocido"})");
                        });
                    }
                    return;
                }

                if (type == "sync-update" && root.TryGetProperty("changes", out var changes))
                {
                    var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                    foreach (var prop in changes.EnumerateObject())
                    {
                        object? boxed = prop.Value.ValueKind switch
                        {
                            JsonValueKind.Number => prop.Value.TryGetDouble(out var d) ? d : (object?)prop.Value.ToString(),
                            JsonValueKind.True => true,
                            JsonValueKind.False => false,
                            JsonValueKind.String => prop.Value.GetString(),
                            _ => prop.Value.ToString()
                        };
                        dict[prop.Name] = boxed;

                        // Supresión por variable para todas las incluidas en el snapshot
                        _suppressUntilByVar[prop.Name] = DateTime.UtcNow.AddMilliseconds(1000);
                    }

                    var incoming = BuildSnapshotFromFlat(dict);

                    WithOutboundSuppressed(() =>
                    {
                        sim.InjectSnapshot(incoming);
                        Logger.Info($"[RemoteSnapshot] {dict.Count} variables aplicadas (desde {remoteSource ?? "desconocido"})");
                    });
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"⚠️ Error aplicando datos remotos: {ex.Message}");
            }
        }

        private void OnLocalSnapshot(SimStateSnapshot snap)
        {
            lock (snapshotLock)
            {
                pendingSnapshot = snap?.Clone() ?? snap;
                hasPendingSnapshot = pendingSnapshot is not null;
            }
        }

        private void OnLocalStateChanged(string variable, object value)
        {
            // Si el cambio proviene de una aplicación REMOTA (bloque síncrono)
            if (Volatile.Read(ref _suppressOutbound) > 0) return;

            // Si la variable está en supresión temporal (por animación async)
            if (_suppressUntilByVar.TryGetValue(variable, out var until) && until > DateTime.UtcNow)
            {
                // Logger.Info($"[LocalChangeSuppressed] {variable} (animación remota)");
                return;
            }

            // Filtro EPS para numéricos: no emitir cambios insignificantes
            if (value is double d)
            {
                var last = _lastSentNumeric.GetOrAdd(variable, double.NaN);
                if (!double.IsNaN(last) && Math.Abs(d - last) < EPS_NUM)
                    return;
                _lastSentNumeric[variable] = d;
            }
            else if (value is float f)
            {
                var d2 = (double)f;
                var last = _lastSentNumeric.GetOrAdd(variable, double.NaN);
                if (!double.IsNaN(last) && Math.Abs(d2 - last) < EPS_NUM)
                    return;
                _lastSentNumeric[variable] = d2;
            }
            else if (value is bool b)
            {
                if (_lastSentBool.TryGetValue(variable, out var lastB) && lastB == b)
                    return;
                _lastSentBool[variable] = b;
            }

            try
            {
                var payload = new
                {
                    type = "aircraft_state",
                    variable,
                    value,
                    source = sourceTag,
                    timestamp = DateTime.UtcNow.ToString("o")
                };
                var json = JsonSerializer.Serialize(payload, jsonOptions);

                if (isHost && hostServer != null)
                {
                    hostServer.Broadcast(json);
                    Logger.Info($"[Broadcast] {FormatVar(variable)} = {FormatValue(value)}");
                }
                else if (ws != null)
                {
                    ws.Send(json);
                    Logger.Info($"[Send] {FormatVar(variable)} = {FormatValue(value)}");
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"⚠️ Error enviando estado individual: {ex.Message}");
            }
        }

        private void StartSynchronizationLoop()
        {
            if (syncTimer != null) return;

            syncTimer = new System.Threading.Timer(_ =>
            {
                try
                {
                    SimStateSnapshot? snapshot;
                    lock (snapshotLock)
                    {
                        if (!hasPendingSnapshot || pendingSnapshot == null) return;
                        snapshot = pendingSnapshot;
                        hasPendingSnapshot = false;
                        pendingSnapshot = null;
                    }

                    var flat = FlattenSnapshot(snapshot);

                    var json = JsonSerializer.Serialize(new
                    {
                        type = "sync-update",
                        changes = flat,
                        source = sourceTag,
                        timestamp = DateTime.UtcNow.ToString("o")
                    }, jsonOptions);

                    if (isHost) hostServer?.Broadcast(json);
                    else ws?.Send(json);

                    // Logger.Info($"[ContinuousSync] Snapshot enviado ({flat.Count} variables)");
                }
                catch (Exception ex)
                {
                    Logger.Warn($"Error en loop de sync: {ex.Message}");
                }
            },
            null,
            TimeSpan.Zero,
            TimeSpan.FromMilliseconds(300));
        }

        private static Dictionary<string, object?> FlattenSnapshot(SimStateSnapshot snap)
        {
            var flat = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            if (!snap.Controls.Equals(default(ControlsStruct)))
            {
                var c = snap.Controls;
                flat["Throttle"] = c.Throttle;
                flat["Flaps"] = c.Flaps;
                flat["GearDown"] = c.GearDown;
                flat["ParkingBrake"] = c.ParkingBrake;
            }

            if (!snap.Systems.Equals(default(SystemsStruct)))
            {
                var s = snap.Systems;
                flat["LightsOn"] = s.LightsOn;
                flat["DoorOpen"] = s.DoorOpen;
                flat["AvionicsOn"] = s.AvionicsOn;
            }

            return flat;
        }

        private static SimStateSnapshot BuildSnapshotFromFlat(Dictionary<string, object?> flat)
        {
            var controls = new ControlsStruct
            {
                Throttle = GetDouble(flat, "Throttle"),
                Flaps = GetDouble(flat, "Flaps"),
                GearDown = GetBool(flat, "GearDown"),
                ParkingBrake = GetBool(flat, "ParkingBrake")
            };

            var systems = new SystemsStruct
            {
                LightsOn = GetBool(flat, "LightsOn"),
                DoorOpen = GetBool(flat, "DoorOpen"),
                AvionicsOn = GetBool(flat, "AvionicsOn")
            };

            return new SimStateSnapshot
            {
                Controls = controls,
                Systems = systems
            };
        }

        private static double GetDouble(IDictionary<string, object?> dict, string key, double fallback = 0)
        {
            if (dict.TryGetValue(key, out var v) && v is not null)
            {
                if (v is double d) return d;
                if (v is float f) return f;
                if (v is int i) return i;
                if (double.TryParse(v.ToString(), out var p)) return p;
            }
            return fallback;
        }

        private static bool GetBool(IDictionary<string, object?> dict, string key, bool fallback = false)
        {
            if (dict.TryGetValue(key, out var v) && v is not null)
            {
                if (v is bool b) return b;
                if (v is int i) return i != 0;
                if (bool.TryParse(v.ToString(), out var p)) return p;
                if (double.TryParse(v.ToString(), out var d)) return Math.Abs(d) > 0.5;
            }
            return fallback;
        }

        private static string FormatValue(object? value)
        {
            if (value is null) return "null";
            return value switch
            {
                bool b => b ? "true" : "false",
                double d => d.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture),
                float f => f.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture),
                _ => value.ToString() ?? "?"
            };
        }

        private static string FormatVar(string v) => v ?? "?";

        private void WithOutboundSuppressed(Action action)
        {
            Interlocked.Increment(ref _suppressOutbound);
            try { action(); }
            finally { Interlocked.Decrement(ref _suppressOutbound); }
        }

        private void Shutdown()
        {
            syncTimer?.Dispose();
            try { ws?.Close(); } catch { }
            try { hostServer?.Stop(); } catch { }
            try { sim.Dispose(); } catch { }
        }

        public void Dispose() => Shutdown();
    }
}
