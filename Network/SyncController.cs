using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
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

        private bool isHost = true;
        private string webSocketUrl = string.Empty;
        private bool warnedNoConnection;
        private string localRole = "pilot";

        private WebSocketManager? ws;
        private WebSocketHost? hostServer;
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
            "controls",
            "systems",
            "cabin",
            "environment",
            "avionics",
            "doors",
            "ground"
        };

        public event Action<IReadOnlyDictionary<string, object?>>? OnSnapshotChanged;
        public event Action<IReadOnlyDictionary<string, object?>>? OnRemoteSyncApplied;

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

            if (!sim.Initialize())
                return;

            StartSynchronizationLoop();

            Logger.Info("Presiona Ctrl+C para cerrar la aplicación.");
            Logger.Info("🌍 Cabina compartida activa: sincronización total entre piloto y copiloto.");
            Logger.Info("🔁 Transmitiendo y aplicando estado del simulador en tiempo real.");

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
            catch (TaskCanceledException)
            {
                // Ignorado: cancelación esperada al cerrar
            }
            finally
            {
                loopToken = null;
            }
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

                Logger.Info("🚀 Modo HOST iniciado.");
                Logger.Info($"Tu dirección local es: {localIp}");
                Logger.Info($"Pídele al copiloto que se conecte a: ws://{localIp}:8081");
                Logger.Info("Esperando conexión del copiloto...");

                localRole = "pilot";
                sim.SetUserRole("PILOT");
            }
            else
            {
                Logger.Info("🌍 Ingresá la IP del piloto:");
                Console.Write("> ");

                var hostInput = Console.ReadLine()?.Trim();

                if (string.IsNullOrWhiteSpace(hostInput))
                    hostInput = "127.0.0.1";

                // ✅ Si el usuario ya puso el puerto, no agregamos otro
                if (!hostInput.StartsWith("ws://"))
                    hostInput = "ws://" + hostInput;

                if (!hostInput.Contains(":"))
                    hostInput += ":8081";

                webSocketUrl = hostInput;
                Logger.Info($"🌍 Intentando conectar con {webSocketUrl}...");

                localRole = "copilot";
                sim.SetUserRole("COPILOT");
            }

            Logger.Info(string.Empty);
        }

        private void SetupWebSocket()
        {
            if (isHost)
            {
                hostServer = new WebSocketHost(8081);

                hostServer.OnClientConnected += (id) =>
                {
                    Logger.Info("🟢 Copiloto conectado correctamente.");
                    warnedNoConnection = false;
                    SendFullSnapshotToClient(id);
                };

                hostServer.OnClientDisconnected += (id) =>
                {
                    Logger.Warn("🔴 Copiloto desconectado.");
                    warnedNoConnection = false;
                };

                hostServer.OnMessage += OnWebSocketMessage;
                hostServer.Start();
            }
            else
            {
                ws = new WebSocketManager(webSocketUrl, sim);

                ws.OnOpen += () =>
                {
                    Logger.Info("✅ Conectado correctamente al host (piloto).");
                    warnedNoConnection = false;
                    SendFullSnapshotToHost();
                };

                ws.OnError += (msg) =>
                {
                    Logger.Error("❌ No se pudo conectar al host. Verificá la IP o que el piloto esté en modo HOST.");
                    if (!string.IsNullOrWhiteSpace(msg))
                        Logger.Warn("Detalles del error: " + msg);
                };

                ws.OnClose += () => Logger.Warn("🔌 Conexión WebSocket cerrada.");
                ws.OnMessage += OnWebSocketMessage;
                ws.Connect();
            }
        }

        private static string GetLocalIpAddress()
        {
            try
            {
                return Dns.GetHostAddresses(Dns.GetHostName())
                    .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork)
                    ?.ToString() ?? "127.0.0.1";
            }
            catch (SocketException ex)
            {
                Logger.Warn($"⚠️ No se pudo obtener la IP local: {ex.Message}");
                return "127.0.0.1";
            }
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
            FlightDisplay.ShowReceivedMessage(message);

            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            if (message.StartsWith("ROLE:", StringComparison.OrdinalIgnoreCase))
            {
                var assignedRole = message.Substring("ROLE:".Length).Trim();
                if (!string.IsNullOrWhiteSpace(assignedRole))
                {
                    sim.SetUserRole(assignedRole);
                }
                return;
            }

            ApplyRemotePayload(message);
        }

        private void Shutdown()
        {
            syncTimer?.Dispose();
            syncTimer = null;

            ws?.Close();
            hostServer?.Stop();
            sim.Dispose();
        }

        public void Dispose()
        {
            sim.OnSnapshot -= OnLocalSnapshot;
            Shutdown();
            loopToken?.Cancel();
            loopToken?.Dispose();
        }

        private void SendFullSnapshotToClient(Guid clientId)
        {
            if (hostServer == null)
            {
                return;
            }

            var snapshot = sim.GetCurrentSnapshot();
            var message = BuildFullSnapshotMessage(snapshot, out var compressedSize);
            if (message == null)
            {
                Logger.Warn("⚠️ No hay estado disponible para sincronizar al nuevo copiloto.");
                return;
            }

            hostServer.SendToClient(clientId, message);
            UpdateLastSentSnapshot(snapshot);
            Logger.Info($"📡 Snapshot completo enviado al copiloto para re-sincronizar. Tamaño comprimido: {compressedSize} bytes.");
        }

        private void SendFullSnapshotToHost()
        {
            if (ws == null)
            {
                return;
            }

            var snapshot = sim.GetCurrentSnapshot();
            var message = BuildFullSnapshotMessage(snapshot, out var compressedSize);
            if (message == null)
            {
                Logger.Warn("⚠️ No hay estado local para enviar al piloto todavía.");
                return;
            }

            ws.Send(message);
            UpdateLastSentSnapshot(snapshot);
            Logger.Info($"📡 Snapshot completo enviado al host para sincronización inicial. Tamaño comprimido: {compressedSize} bytes.");
        }

        private void StartSynchronizationLoop()
        {
            if (syncTimer != null)
            {
                return;
            }

            syncTimer = new System.Threading.Timer(OnSyncTimerTick, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(100));
            Logger.Info($"🕒 Temporizador de sincronización iniciado. sourceTag={sourceTag}");
        }

        private void OnLocalSnapshot(SimStateSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return;
            }

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
                    if (!hasPendingSnapshot || pendingSnapshot == null)
                    {
                        return;
                    }

                    snapshot = pendingSnapshot;
                    pendingSnapshot = null;
                    hasPendingSnapshot = false;
                }

                var diff = ComputeDiff(snapshot, lastSentSnapshot);
                if (diff.Count == 0)
                {
                    return;
                }

                SendSyncUpdate(diff, snapshot);
            }
            catch (Exception ex)
            {
                Logger.Warn($"⚠️ Error en bucle de sincronización: {ex.Message}");
            }
        }

        private void SendSyncUpdate(IReadOnlyDictionary<string, object?> changes, SimStateSnapshot snapshot)
        {
            OnSnapshotChanged?.Invoke(changes);

            var payload = new
            {
                type = "sync-update",
                source = sourceTag,
                timestamp = DateTime.UtcNow.ToString("o"),
                changes
            };

            var json = JsonSerializer.Serialize(payload, jsonOptions);

            BroadcastPayload(json);
            UpdateLastSentSnapshot(snapshot);
            Logger.Info($"🔼 Enviando {changes.Count} cambios al otro extremo.");
        }

        private void BroadcastPayload(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
            {
                return;
            }

            if (isHost)
            {
                if (hostServer != null && hostServer.HasClients)
                {
                    hostServer.Broadcast(payload);
                    FlightDisplay.ShowSentToCopilot(payload);
                    warnedNoConnection = false;
                }
                else if (!warnedNoConnection)
                {
                    Logger.Info("⌛ Aún no hay copilotos conectados. Los datos se enviarán automáticamente en cuanto se unan.");
                    warnedNoConnection = true;
                }
            }
            else
            {
                if (ws != null)
                {
                    ws.Send(payload);
                    FlightDisplay.ShowSentToHost(payload);
                    warnedNoConnection = false;
                }
                else if (!warnedNoConnection)
                {
                    Logger.Warn("⚠️ No se puede enviar datos porque el WebSocket no está conectado.");
                    warnedNoConnection = true;
                }
            }
        }

        private Dictionary<string, object?> ComputeDiff(SimStateSnapshot current, SimStateSnapshot? previous)
        {
            var currentFlat = FlattenSnapshot(current);
            var previousFlat = previous != null
                ? FlattenSnapshot(previous)
                : new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            var diff = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            foreach (var kv in currentFlat)
            {
                if (!previousFlat.TryGetValue(kv.Key, out var prevValue) || !ValuesEqual(prevValue, kv.Value))
                {
                    diff[kv.Key] = kv.Value;
                }
            }

            foreach (var removed in previousFlat.Keys.Except(currentFlat.Keys, StringComparer.OrdinalIgnoreCase))
            {
                diff[removed] = null;
            }

            return diff;
        }

        private static Dictionary<string, object?> FlattenSnapshot(SimStateSnapshot snapshot)
        {
            var data = snapshot.ToDictionary();
            var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            foreach (var group in data)
            {
                if (!SyncGroups.Contains(group.Key))
                {
                    continue;
                }

                if (group.Value is IReadOnlyDictionary<string, object?> nestedRead)
                {
                    foreach (var inner in nestedRead)
                    {
                        result[$"{group.Key}.{inner.Key}"] = inner.Value;
                    }
                }
                else if (group.Value is IDictionary<string, object?> nestedDict)
                {
                    foreach (var inner in nestedDict)
                    {
                        result[$"{group.Key}.{inner.Key}"] = inner.Value;
                    }
                }
            }

            return result;
        }

        private static bool ValuesEqual(object? a, object? b)
        {
            if (a == null && b == null)
            {
                return true;
            }

            if (a == null || b == null)
            {
                return false;
            }

            if (TryAsDouble(a, out var da) && TryAsDouble(b, out var db))
            {
                return Math.Abs(da - db) < 0.0001;
            }

            if (TryAsBool(a, out var ba) && TryAsBool(b, out var bb))
            {
                return ba == bb;
            }

            return string.Equals(
                Convert.ToString(a, CultureInfo.InvariantCulture),
                Convert.ToString(b, CultureInfo.InvariantCulture),
                StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryAsDouble(object? value, out double result)
        {
            switch (value)
            {
                case null:
                    result = 0;
                    return false;
                case double d:
                    result = d;
                    return true;
                case float f:
                    result = f;
                    return true;
                case int i:
                    result = i;
                    return true;
                case long l:
                    result = l;
                    return true;
                case decimal m:
                    result = (double)m;
                    return true;
                case string s when double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed):
                    result = parsed;
                    return true;
                default:
                    result = 0;
                    return false;
            }
        }

        private static bool TryAsBool(object? value, out bool result)
        {
            switch (value)
            {
                case null:
                    result = false;
                    return false;
                case bool b:
                    result = b;
                    return true;
                case int i:
                    result = i != 0;
                    return true;
                case long l:
                    result = l != 0;
                    return true;
                case double d:
                    result = Math.Abs(d) > double.Epsilon;
                    return true;
                case string s when bool.TryParse(s, out var parsedBool):
                    result = parsedBool;
                    return true;
                case string s when int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedInt):
                    result = parsedInt != 0;
                    return true;
                default:
                    result = false;
                    return false;
            }
        }

        private Dictionary<string, object?> ParseChanges(JsonElement element)
        {
            var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            foreach (var property in element.EnumerateObject())
            {
                var key = property.Name;
                var group = key.Split('.', 2)[0];
                if (!SyncGroups.Contains(group))
                {
                    continue;
                }

                result[key] = ReadJsonElement(property.Value);
            }

            return result;
        }

        private static object? ReadJsonElement(JsonElement element) => element.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Number when element.TryGetInt64(out var intValue) => intValue,
            JsonValueKind.Number => element.GetDouble(),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Null => null,
            _ => element.ToString()
        };

        private SimStateSnapshot? BuildSnapshotFromJson(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var snapshot = new SimStateSnapshot();
                var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

                foreach (var property in doc.RootElement.EnumerateObject())
                {
                    FlattenJsonElement(property.Value, property.Name, values);
                }

                foreach (var kv in values)
                {
                    if (!snapshot.TryApplyChange(kv.Key, kv.Value))
                    {
                        Logger.Warn($"⚠️ No se pudo aplicar parte del snapshot remoto: {kv.Key}");
                    }
                }

                return snapshot;
            }
            catch (JsonException ex)
            {
                Logger.Warn($"⚠️ Snapshot remoto con formato inválido: {ex.Message}");
                return null;
            }
        }

        private void FlattenJsonElement(JsonElement element, string prefix, IDictionary<string, object?> result)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                foreach (var child in element.EnumerateObject())
                {
                    FlattenJsonElement(child.Value, $"{prefix}.{child.Name}", result);
                }
                return;
            }

            var group = prefix.Split('.', 2)[0];
            if (!SyncGroups.Contains(group))
            {
                return;
            }

            result[prefix] = ReadJsonElement(element);
        }

        private string? BuildFullSnapshotMessage(SimStateSnapshot snapshot, out int compressedSize)
        {
            compressedSize = 0;
            var data = snapshot.ToDictionary();
            if (data.Count == 0)
            {
                return null;
            }

            var json = JsonSerializer.Serialize(data, jsonOptions);
            var base64 = CompressToBase64(json, out compressedSize);

            var payload = new
            {
                type = "sync-full",
                source = sourceTag,
                timestamp = DateTime.UtcNow.ToString("o"),
                dataCompressed = base64
            };

            var message = JsonSerializer.Serialize(payload, jsonOptions);
            Logger.Info($"📡 Sync delta size: {FlattenSnapshot(snapshot).Count} | Compressed snapshot: {compressedSize} bytes");
            return message;
        }

        private static string CompressToBase64(string json, out int byteSize)
        {
            using var output = new MemoryStream();
            using (var gzip = new GZipStream(output, CompressionLevel.Fastest, leaveOpen: true))
            using (var writer = new StreamWriter(gzip, Encoding.UTF8))
            {
                writer.Write(json);
            }

            var bytes = output.ToArray();
            byteSize = bytes.Length;
            return Convert.ToBase64String(bytes);
        }

        private static string? DecompressSnapshot(string base64)
        {
            try
            {
                var bytes = Convert.FromBase64String(base64);
                using var input = new MemoryStream(bytes);
                using var gzip = new GZipStream(input, CompressionMode.Decompress);
                using var reader = new StreamReader(gzip, Encoding.UTF8);
                return reader.ReadToEnd();
            }
            catch (Exception ex)
            {
                Logger.Warn($"⚠️ No se pudo descomprimir snapshot remoto: {ex.Message}");
                return null;
            }
        }

        private void UpdateLastSentSnapshot(SimStateSnapshot snapshot)
        {
            lock (snapshotLock)
            {
                lastSentSnapshot = snapshot.Clone();
            }
        }

        private void ApplyRemotePayload(string message)
        {
            try
            {
                using var doc = JsonDocument.Parse(message);
                var root = doc.RootElement;

                if (!root.TryGetProperty("type", out var typeElement))
                {
                    Logger.Warn("⚠️ Mensaje remoto sin tipo de sincronización.");
                    return;
                }

                var type = typeElement.GetString();
                var source = root.TryGetProperty("source", out var sourceElement)
                    ? sourceElement.GetString()
                    : null;

                if (!string.IsNullOrWhiteSpace(source) &&
                    string.Equals(source, sourceTag, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                switch (type)
                {
                    case "sync-update":
                        if (!root.TryGetProperty("changes", out var changesElement) ||
                            changesElement.ValueKind != JsonValueKind.Object)
                        {
                            Logger.Warn("⚠️ Mensaje de actualización sin cambios válidos.");
                            return;
                        }

                        var changes = ParseChanges(changesElement);
                        if (changes.Count == 0)
                        {
                            return;
                        }

                        if (sim.ApplyRemoteChanges(changes))
                        {
                            Logger.Info($"📥 Cambios remotos aplicados ({changes.Count}).");
                            OnRemoteSyncApplied?.Invoke(changes);
                            UpdateLastSentSnapshot(sim.GetCurrentSnapshot());
                        }
                        break;

                    case "sync-full":
                        if (!root.TryGetProperty("dataCompressed", out var dataElement))
                        {
                            Logger.Warn("⚠️ Snapshot completo recibido sin datos comprimidos.");
                            return;
                        }

                        var compressed = dataElement.GetString();
                        if (string.IsNullOrWhiteSpace(compressed))
                        {
                            Logger.Warn("⚠️ Datos comprimidos vacíos en snapshot completo.");
                            return;
                        }

                        var json = DecompressSnapshot(compressed);
                        if (string.IsNullOrWhiteSpace(json))
                        {
                            Logger.Warn("⚠️ No se pudo descomprimir el snapshot remoto.");
                            return;
                        }

                        var snapshot = BuildSnapshotFromJson(json);
                        if (snapshot == null)
                        {
                            Logger.Warn("⚠️ Snapshot remoto inválido, se omite.");
                            return;
                        }

                        if (sim.ApplyRemoteFullSnapshot(snapshot))
                        {
                            Logger.Info("📦 Snapshot completo remoto aplicado tras reconexión.");
                            var flattened = FlattenSnapshot(snapshot);
                            OnRemoteSyncApplied?.Invoke(flattened);
                            UpdateLastSentSnapshot(snapshot);
                        }
                        break;

                    default:
                        Logger.Warn($"⚠️ Tipo de mensaje de sincronización desconocido: {type}");
                        break;
                }
            }
            catch (JsonException ex)
            {
                Logger.Warn($"⚠️ Mensaje remoto inválido: {ex.Message}");
            }
            catch (Exception ex)
            {
                Logger.Error($"⚠️ Error procesando mensaje remoto: {ex.Message}");
            }
        }
    }
}
