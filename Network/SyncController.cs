using System;
using System.Collections.Generic;
using System.IO;
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
#pragma warning disable 0414, 0169 // Evita advertencias por campos no usados
    public sealed class SyncController : IDisposable
    {
        private readonly SimConnectManager sim;
        private bool isHost = true;
        private string webSocketUrl = string.Empty;
        private string localRole = "pilot";
        private bool warnedNoConnection = false; // Inicializado para evitar advertencias

        private WebSocketManager? ws;
        private WebSocketHost? hostServer;
        private NetworkDiscovery? discovery;
        private CancellationTokenSource? loopToken;
        private System.Threading.Timer? syncTimer;

        private readonly object snapshotLock = new();
        private SimStateSnapshot? pendingSnapshot;
        private SimStateSnapshot? lastSentSnapshot;
        private bool hasPendingSnapshot;

        private readonly string sourceTag = Guid.NewGuid().ToString();
        private readonly JsonSerializerOptions jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private static readonly HashSet<string> SyncGroups = new(StringComparer.OrdinalIgnoreCase)
        {
            "controls","systems","cabin","environment","avionics","doors","ground"
        };

        public SyncController(SimConnectManager sim)
        {
            this.sim = sim ?? throw new ArgumentNullException(nameof(sim));
            this.sim.OnSnapshot += OnLocalSnapshot;
        }

        public async Task RunAsync()
        {
            Console.OutputEncoding = Encoding.UTF8;
            ConfigureMode();
            SetupWebSocket();
            SetupShutdownHandlers();

            // 🧩 Detección automática de MSFS2024
            bool msfsRunning = false;
            try
            {
                msfsRunning = System.Diagnostics.Process.GetProcessesByName("FlightSimulator").Any();
                if (!msfsRunning)
                {
                    Logger.Warn("🧩 No se detectó MSFS2024. Ejecutando en modo simulación interna.");
                    sim.EnableMockMode();
                }
                else
                {
                    Logger.Info("🛫 MSFS2024 detectado. Inicializando conexión real con SimConnect...");
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"⚠️ Error detectando MSFS2024: {ex.Message}");
                sim.EnableMockMode();
            }

            Logger.Info($"🟢 Modo de simulación activo: {(msfsRunning ? "Real (SimConnect)" : "Interno (Mock)")}");

            // ✅ Inicialización automática
            sim.Initialize(IntPtr.Zero);
            Logger.Info("✅ Inicialización de SimConnect completada (modo automático).");

            StartSynchronizationLoop();
            Logger.Info("Presiona Ctrl+C para cerrar la aplicación.");
            Logger.Info("🌍 Cabina compartida activa: sincronización total entre piloto y copiloto.");

            using var cts = new CancellationTokenSource();
            loopToken = cts;
            try
            {
                while (!cts.IsCancellationRequested)
                {
                    sim.ReceiveMessage();
                    await Task.Delay(100, cts.Token);
                }
            }
            catch (TaskCanceledException) { }
        }

        private void ConfigureMode()
        {
            Logger.Info("Selecciona modo de operación:");
            Logger.Info("1) Host (piloto principal)");
            Logger.Info("2) Cliente (copiloto)");
            Console.Write("> ");
            var option = Console.ReadLine();
            isHost = option?.Trim() != "2";

            if (isHost)
            {
                var localIp = GetLocalIpAddress();
                Logger.Info("✈️ Configurando sesión de vuelo...");
                Console.Write("🪪 Nombre de cabina compartida: ");
                var sessionName = Console.ReadLine()?.Trim();
                if (string.IsNullOrWhiteSpace(sessionName))
                    sessionName = "Cabina_" + Environment.MachineName;

                Console.Write("🔓 ¿Hacer la cabina pública? (S/N): ");
                var pub = Console.ReadLine()?.Trim().ToUpperInvariant() == "S";
                string password = "";
                if (!pub)
                {
                    Console.Write("🔑 Ingrese una contraseña para la cabina: ");
                    password = Console.ReadLine() ?? "";
                }

                discovery = new NetworkDiscovery(sessionName, localIp, 8081);
                discovery.StartBroadcast(pub, password);

                Logger.Info($"🚀 Cabina '{sessionName}' iniciada en {(pub ? "modo Público" : "Privado 🔒")}.");
                Logger.Info($"🛰️ Dirección: ws://{localIp}:8081");
                Logger.Info("Esperando conexión del copiloto...");

                localRole = "pilot";
                sim.SetUserRole("PILOT");
            }
            else
            {
                discovery = new NetworkDiscovery(Environment.MachineName, "0.0.0.0", 8081);
                discovery.StartListening();

                var available = new List<(string Name, string Ip, bool IsPublic)>();
                discovery.OnHostDiscovered += (ip, name, pub) =>
                {
                    if (available.All(a => a.Ip != ip))
                        available.Add((name, ip, pub));
                };

                Logger.Info("🔍 Buscando cabinas compartidas activas en la red local...");
                int waitCounter = 0;
                while (available.Count == 0)
                {
                    waitCounter++;
                    Logger.Info($"⏳ Esperando que aparezcan sesiones... (Intento {waitCounter})");
                    Thread.Sleep(3000);
                }

                Logger.Info("📡 Cabinas detectadas:");
                for (int i = 0; i < available.Count; i++)
                {
                    var mode = available[i].IsPublic ? "Pública" : "Privada 🔒";
                    Logger.Info($"{i + 1}) {available[i].Name} ({mode}) - {available[i].Ip}");
                }

                Console.Write("👉 Ingresá el número o nombre de la cabina a la que querés conectarte: ");
                var choice = Console.ReadLine()?.Trim();

                (string Name, string Ip, bool IsPublic)? selected = null;
                if (int.TryParse(choice, out var num) && num > 0 && num <= available.Count)
                    selected = available[num - 1];
                else
                    selected = available.FirstOrDefault(a => a.Name.Equals(choice, StringComparison.OrdinalIgnoreCase));

                if (selected == null)
                {
                    Logger.Error("❌ Cabina no encontrada. Volvé a intentarlo.");
                    Environment.Exit(0);
                }

                if (!selected.Value.IsPublic)
                {
                    Console.Write("🔑 Ingresá la contraseña: ");
                    var entered = Console.ReadLine() ?? "";
                    if (!discovery.ValidatePassword(selected.Value.Name, entered))
                    {
                        Logger.Error("🚫 Contraseña incorrecta. No se puede conectar a esta cabina.");
                        Environment.Exit(0);
                    }
                }

                webSocketUrl = $"ws://{selected.Value.Ip}:8081";
                Logger.Info($"🌍 Intentando conectar con {webSocketUrl}...");
                localRole = "copilot";
                sim.SetUserRole("COPILOT");
            }
        }

        private void SetupWebSocket()
        {
            if (isHost)
            {
                hostServer = new WebSocketHost(8081);
                hostServer.OnClientConnected += (id) =>
                {
                    Logger.Info("🟢 Copiloto conectado correctamente.");
                    SendFullSnapshotToClient(id);
                };
                hostServer.OnClientDisconnected += (id) => Logger.Warn("🔴 Copiloto desconectado.");
                hostServer.OnMessage += OnWebSocketMessage;
                hostServer.Start();
            }
            else
            {
                ws = new WebSocketManager(webSocketUrl, sim);
                ws.OnOpen += () => { Logger.Info("✅ Conectado correctamente al host (piloto)."); SendFullSnapshotToHost(); };
                ws.OnError += (msg) => Logger.Error("❌ No se pudo conectar al host: " + msg);
                ws.OnClose += () => Logger.Warn("🔌 Conexión cerrada.");
                ws.OnMessage += OnWebSocketMessage;
                ws.Connect();
            }
        }

        private static string GetLocalIpAddress()
        {
            try
            {
                return Dns.GetHostAddresses(Dns.GetHostName())
                    .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork)?.ToString() ?? "127.0.0.1";
            }
            catch { return "127.0.0.1"; }
        }

        private void SetupShutdownHandlers()
        {
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                Shutdown();
                loopToken?.Cancel();
                Environment.Exit(0);
            };
            AppDomain.CurrentDomain.ProcessExit += (_, __) => Shutdown();
        }

        private void OnWebSocketMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;
            if (message.StartsWith("ROLE:", StringComparison.OrdinalIgnoreCase))
            {
                sim.SetUserRole(message.Substring(5).Trim());
                return;
            }
            ApplyRemotePayload(message);
        }

        private void Shutdown()
        {
            syncTimer?.Dispose();
            try { ws?.Close(); } catch { }
            try { hostServer?.Stop(); } catch { }
            try { sim.Dispose(); } catch { }
            discovery?.Stop();
        }

        public void Dispose()
        {
            sim.OnSnapshot -= OnLocalSnapshot;
            Shutdown();
        }

        private void StartSynchronizationLoop()
        {
            if (syncTimer != null) return;
            syncTimer = new System.Threading.Timer(OnSyncTimerTick, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(100));
        }

        private void OnLocalSnapshot(SimStateSnapshot snapshot)
        {
            if (snapshot == null) return;
            lock (snapshotLock)
            {
                pendingSnapshot = snapshot.Clone();
                hasPendingSnapshot = true;
            }
        }

        private void OnSyncTimerTick(object? state)
        {
            try
            {
                SimStateSnapshot? snapshot;
                lock (snapshotLock)
                {
                    if (!hasPendingSnapshot || pendingSnapshot == null) return;
                    snapshot = pendingSnapshot;
                    pendingSnapshot = null;
                    hasPendingSnapshot = false;
                }
                var diff = ComputeDiff(snapshot, lastSentSnapshot);
                if (diff.Count == 0) return;
                SendSyncUpdate(diff, snapshot);
            }
            catch (Exception ex)
            {
                Logger.Warn($"⚠️ Error en bucle de sincronización: {ex.Message}");
            }
        }

        private void SendSyncUpdate(IReadOnlyDictionary<string, object?> changes, SimStateSnapshot snapshot)
        {
            var payload = new { type = "sync-update", source = sourceTag, timestamp = DateTime.UtcNow.ToString("o"), changes };
            var json = JsonSerializer.Serialize(payload, jsonOptions);
            BroadcastPayload(json);
            lastSentSnapshot = snapshot.Clone();
        }

        private void BroadcastPayload(string payload)
        {
            if (isHost)
                hostServer?.Broadcast(payload);
            else
                ws?.Send(payload);
        }

        private Dictionary<string, object?> ComputeDiff(SimStateSnapshot current, SimStateSnapshot? previous)
        {
            var cur = FlattenSnapshot(current);
            var prev = previous != null ? FlattenSnapshot(previous) : new();
            var diff = new Dictionary<string, object?>();
            foreach (var kv in cur)
                if (!prev.TryGetValue(kv.Key, out var old) || !Equals(old, kv.Value))
                    diff[kv.Key] = kv.Value;
            return diff;
        }

        private static Dictionary<string, object?> FlattenSnapshot(SimStateSnapshot snapshot)
        {
            var data = snapshot.ToDictionary();
            var flat = new Dictionary<string, object?>();
            foreach (var group in data)
                if (SyncGroups.Contains(group.Key) && group.Value is IDictionary<string, object?> d)
                    foreach (var kv in d) flat[$"{group.Key}.{kv.Key}"] = kv.Value;
            return flat;
        }

        private void ApplyRemotePayload(string message)
        {
            try
            {
                using var doc = JsonDocument.Parse(message);
                var root = doc.RootElement;
                var type = root.GetProperty("type").GetString();
                if (type == "sync-update" && root.TryGetProperty("changes", out var changesElement))
                {
                    var changes = changesElement.EnumerateObject().ToDictionary(p => p.Name, p => (object?)p.Value.ToString());
                    sim.ApplyRemoteChanges(changes);
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"⚠️ Error aplicando datos remotos: {ex.Message}");
            }
        }

        private void SendFullSnapshotToClient(Guid id)
        {
            var s = sim.GetCurrentSnapshot();
            var json = JsonSerializer.Serialize(s.ToDictionary(), jsonOptions);
            hostServer?.SendToClient(id, json);
        }

        private void SendFullSnapshotToHost()
        {
            var s = sim.GetCurrentSnapshot();
            var json = JsonSerializer.Serialize(s.ToDictionary(), jsonOptions);
            ws?.Send(json);
        }
    }
#pragma warning restore 0414, 0169
}
