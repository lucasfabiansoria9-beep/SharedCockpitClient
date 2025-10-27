using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
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

            // Detectar MSFS y forzar mock si no está
            try
            {
                var msfsRunning = System.Diagnostics.Process.GetProcessesByName("FlightSimulator").Any();
                if (!msfsRunning) sim.EnableMockMode();
            }
            catch
            {
                sim.EnableMockMode();
            }

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
                        // Si tu SimConnectManager expone ReceiveMessage, mantenemos el loop
                        sim.ReceiveMessage();
                        await Task.Delay(100, cts.Token);
                    }
                }
                catch (TaskCanceledException) { }
            });

            // Bucle de prueba manual
            RunManualTestLoop();
        }

        // ─────────────────────────────────────────────────────────────────────────────
        // MODO MANUAL (LAB)
        // ─────────────────────────────────────────────────────────────────────────────
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

        // ─────────────────────────────────────────────────────────────────────────────
        // CONFIGURACIÓN
        // ─────────────────────────────────────────────────────────────────────────────
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
                // Simplificado: seteamos una IP manual (ajustá si querés)
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

        // ─────────────────────────────────────────────────────────────────────────────
        // RECEPCIÓN Y ENVÍO
        // ─────────────────────────────────────────────────────────────────────────────
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

                if (!root.TryGetProperty("type", out var typeProp))
                    return;

                var type = typeProp.GetString();

                // Cambios puntuales: aircraft_state
                if (type == "aircraft_state" &&
                    root.TryGetProperty("variable", out var v) &&
                    root.TryGetProperty("value", out var val))
                {
                    var variable = v.GetString();
                    if (string.IsNullOrEmpty(variable)) return;

                    var source = root.TryGetProperty("source", out var sourceProp)
                        ? sourceProp.GetString()
                        : null;

                    if (!string.IsNullOrWhiteSpace(source) && string.Equals(source, sourceTag, StringComparison.Ordinal))
                        return;

                    aircraftState.ApplyRemoteChange(variable, val);
                    var formattedValue = val.ValueKind == JsonValueKind.Undefined ? "(undefined)" : val.ToString();
                    Logger.Info($"[RemoteChange] {variable} = {formattedValue} (desde {source ?? "desconocido"})");
                    return;
                }

                // Actualización de snapshot (plano): sync-update
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
                    }

                    // Mapear diccionario plano -> snapshot y aplicarlo
                    var incoming = BuildSnapshotFromFlat(dict);
                    sim.InjectSnapshot(incoming);
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
                // Si tu SimStateSnapshot tiene Clone(), perfecto; sino, podemos asignar directo.
                pendingSnapshot = snap?.Clone() ?? snap;
                hasPendingSnapshot = pendingSnapshot is not null;
            }
        }

        private void OnLocalStateChanged(string variable, object value)
        {
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
                if (isHost)
                {
                    hostServer?.Broadcast(json);
                    Logger.Info($"[Broadcast] {variable} = {FormatValue(value)}");
                }
                else
                {
                    ws?.Send(json);
                    Logger.Info($"[Send] {variable} = {FormatValue(value)}");
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"⚠️ Error enviando estado individual: {ex.Message}");
            }
        }

        private static string FormatValue(object value)
        {
            return value switch
            {
                null => "<null>",
                JsonElement jsonElement => jsonElement.ValueKind == JsonValueKind.Undefined ? "(undefined)" : jsonElement.ToString(),
                bool b => b ? "true" : "false",
                double d => d.ToString(CultureInfo.InvariantCulture),
                float f => f.ToString(CultureInfo.InvariantCulture),
                IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
                _ => value.ToString() ?? string.Empty
            };
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

                    // 🔁 Enviar snapshot FLAT para que el peer lo entienda
                    var flat = FlattenSnapshot(snapshot);

                    var json = JsonSerializer.Serialize(new
                    {
                        type = "sync-update",
                        changes = flat
                    }, jsonOptions);

                    if (isHost) hostServer?.Broadcast(json);
                    else ws?.Send(json);
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

        // Pasar el snapshot (con grupos) a un diccionario plano
        private static Dictionary<string, object?> FlattenSnapshot(SimStateSnapshot snap)
        {
            var flat = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            // ControlsStruct (struct no-nullable)
            if (!snap.Controls.Equals(default(ControlsStruct)))
            {
                var c = snap.Controls;
                flat["throttle"] = c.Throttle;
                flat["flaps"] = c.Flaps;
                flat["gearDown"] = c.GearDown;
                flat["parkingBrake"] = c.ParkingBrake;
            }

            // SystemsStruct (struct no-nullable)
            if (!snap.Systems.Equals(default(SystemsStruct)))
            {
                var s = snap.Systems;
                flat["lightsOn"] = s.LightsOn;
                flat["doorOpen"] = s.DoorOpen;
                flat["avionicsOn"] = s.AvionicsOn;
            }

            return flat;
        }

        // Construye un snapshot desde un diccionario plano "flat"
        private static SimStateSnapshot BuildSnapshotFromFlat(Dictionary<string, object?> flat)
        {
            var controls = new ControlsStruct
            {
                Throttle = GetDouble(flat, "throttle"),
                Flaps = GetDouble(flat, "flaps"),
                GearDown = GetBool(flat, "gearDown"),
                ParkingBrake = GetBool(flat, "parkingBrake")
            };

            var systems = new SystemsStruct
            {
                LightsOn = GetBool(flat, "lightsOn"),
                DoorOpen = GetBool(flat, "doorOpen"),
                AvionicsOn = GetBool(flat, "avionicsOn")
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

        // ─────────────────────────────────────────────────────────────────────────────
        // SHUTDOWN
        // ─────────────────────────────────────────────────────────────────────────────
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
