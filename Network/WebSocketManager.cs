using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace SharedCockpitClient.Network
{
    /// <summary>
    /// Gestor WebSocket bidireccional compatible con host y cliente.
    /// Implementa el bus requerido por SyncController con anti-eco y sellado serverTime.
    /// </summary>
    public sealed class WebSocketManager : INetworkBus, IDisposable
    {
        private readonly bool isHost;
        private readonly Uri? peerUri;
        private readonly int? configuredPort;
        private WebSocketHost? host;
        private ClientWebSocket? client;
        private CancellationTokenSource? linkedCts;
        private bool clientConnected;
        private CancellationTokenSource? pingCts;
        private readonly Queue<double> rttSamples = new();
        private readonly object rttLock = new();
        private double averageRtt;
        private readonly TaskCompletionSource<bool> readyTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public event Action<string>? OnMessage;
        public event Action<string?, string?, long, Dictionary<string, object?>>? OnStateDiff;
        public event Action<CommandPayload>? OnCommand;

        public WebSocketManager(bool isHost, Uri? peer = null, int? portOverride = null)
        {
            this.isHost = isHost;
            peerUri = peer;
            configuredPort = portOverride;
        }

        public bool IsConnected => isHost
            ? host?.HasClients == true
            : clientConnected && client?.State == WebSocketState.Open;

        public double AverageRttMs
        {
            get
            {
                lock (rttLock)
                {
                    return averageRtt;
                }
            }
        }

        public double AverageLagMs => AverageRttMs;

        public Task Ready => readyTcs.Task;

        public async Task StartAsync(CancellationToken ct)
        {
            if (isHost)
            {
                var listenPort = configuredPort ?? peerUri?.Port ?? 8081;
                host = new WebSocketHost(listenPort);
                host.OnMessage += HandleHostMessage;
                host.OnClientConnected += id => Console.WriteLine($"[WebSocket] Cliente conectado ({id})");
                host.OnClientDisconnected += id => Console.WriteLine($"[WebSocket] Cliente desconectado ({id})");

                _ = Task.Run(() =>
                {
                    try
                    {
                        host.Start();
                        Console.WriteLine($"[WebSocket] Host escuchando en ws://0.0.0.0:{listenPort}");
                        readyTcs.TrySetResult(true);
                    }
                    catch (Exception ex)
                    {
                        readyTcs.TrySetException(ex);
                        Console.WriteLine($"[WebSocket] ❌ Error iniciando host: {ex.Message}");
                    }
                }, ct);

                StartPingLoop(ct);
                await Task.CompletedTask;
                return;
            }

            linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            client = new ClientWebSocket();
            try
            {
                await client.ConnectAsync(peerUri ?? throw new InvalidOperationException("Peer no especificado para modo cliente."), ct)
                    .ConfigureAwait(false);
                Console.WriteLine($"[WebSocket] Conectado a {peerUri}");
                clientConnected = true;
                readyTcs.TrySetResult(true);
                _ = Task.Run(() => ReceiveLoopAsync(client, linkedCts.Token));
                StartPingLoop(linkedCts.Token);
            }
            catch (Exception ex)
            {
                readyTcs.TrySetException(ex);
                throw;
            }
        }

        public async Task SendAsync(string json)
        {
            if (isHost)
            {
                await BroadcastAsync(json).ConfigureAwait(false);
                return;
            }

            var ws = client;
            if (ws == null || ws.State != WebSocketState.Open)
                return;

            var bytes = Encoding.UTF8.GetBytes(json);
            await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None)
                .ConfigureAwait(false);
        }

        public async Task BroadcastAsync(string json)
        {
            if (!isHost || host is null)
                return;

            if (TryStampServerTime(json, out var stamped))
            {
                json = stamped;
            }

            host.Broadcast(json);
            if (!IsInternalPayload(json))
            {
                Console.WriteLine("[Broadcast] Rebroadcast a clientes (serverTime sellado)");
            }
            await Task.CompletedTask;
        }

        private void HandleHostMessage(string json)
        {
            if (TryHandleInternalMessage(json))
                return;

            if (TryStampServerTime(json, out var stamped))
            {
                json = stamped;
            }

            host?.Broadcast(json);
            if (!IsInternalPayload(json))
            {
                Console.WriteLine("[Broadcast] Rebroadcast a clientes (serverTime sellado)");
            }
            DispatchMessage(json);
        }

        private static bool TryStampServerTime(string json, out string stamped)
        {
            stamped = json;
            try
            {
                var msg = JsonSerializer.Deserialize<StateChangeMessage>(json);
                if (msg != null)
                {
                    msg.serverTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    stamped = JsonSerializer.Serialize(msg);
                    return true;
                }
            }
            catch
            {
                // Mensaje no compatible; no forzamos formato.
            }

            return false;
        }

        private async Task ReceiveLoopAsync(ClientWebSocket ws, CancellationToken ct)
        {
            var buffer = new byte[64 * 1024];
            while (!ct.IsCancellationRequested && ws.State == WebSocketState.Open)
            {
                var builder = new StringBuilder();
                WebSocketReceiveResult result;
                do
                {
                    result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct).ConfigureAwait(false);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        clientConnected = false;
                        return;
                    }
                    if (result.MessageType != WebSocketMessageType.Text)
                        continue;

                    builder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                }
                while (!result.EndOfMessage);

                if (builder.Length > 0)
                {
                    var message = MaybeDecompress(builder.ToString());
                    DispatchMessage(message);
                }
            }

            clientConnected = false;
        }

        private static string MaybeDecompress(string message)
        {
            if (!message.StartsWith("gz:", StringComparison.Ordinal))
                return message;

            try
            {
                var compressed = Convert.FromBase64String(message[3..]);
                using var input = new System.IO.MemoryStream(compressed);
                using var gzip = new System.IO.Compression.GZipStream(input, System.IO.Compression.CompressionMode.Decompress);
                using var reader = new System.IO.StreamReader(gzip, Encoding.UTF8);
                return reader.ReadToEnd();
            }
            catch
            {
                return message;
            }
        }

        private void DispatchMessage(string json)
        {
            if (TryHandleInternalMessage(json))
                return;

            OnMessage?.Invoke(json);
            TryEmitStateDiff(json);
            TryEmitCommand(json);
        }

        private bool TryHandleInternalMessage(string json)
        {
            try
            {
                using var document = JsonDocument.Parse(json);
                var root = document.RootElement;
                if (!root.TryGetProperty("type", out var typeProperty))
                    return false;

                var type = typeProperty.GetString();
                if (string.Equals(type, "ping", StringComparison.OrdinalIgnoreCase))
                {
                    if (root.TryGetProperty("t", out var tProp) && tProp.ValueKind == JsonValueKind.Number)
                    {
                        var ticks = tProp.GetInt64();
                        _ = SendInternalAsync("pong", ticks);
                    }
                    return true;
                }

                if (string.Equals(type, "pong", StringComparison.OrdinalIgnoreCase))
                {
                    if (root.TryGetProperty("t", out var tProp) && tProp.ValueKind == JsonValueKind.Number)
                    {
                        var ticks = tProp.GetInt64();
                        try
                        {
                            var sentUtc = new DateTime(ticks, DateTimeKind.Utc);
                            var rtt = (DateTime.UtcNow - sentUtc).TotalMilliseconds;
                            if (rtt > 0 && rtt < 60000)
                                RegisterRtt(rtt);
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            // ticks inválidos, ignorar
                        }
                    }
                    return true;
                }
            }
            catch (JsonException)
            {
                return false;
            }

            return false;
        }

        private void TryEmitStateDiff(string json)
        {
            try
            {
                using var document = JsonDocument.Parse(json);
                var root = document.RootElement;
                if (!root.TryGetProperty("type", out var typeProperty) ||
                    !string.Equals(typeProperty.GetString(), "state-diff", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                string? role = null;
                if (root.TryGetProperty("role", out var roleProperty) && roleProperty.ValueKind == JsonValueKind.String)
                {
                    role = roleProperty.GetString();
                }

                string? originId = null;
                if (root.TryGetProperty("originId", out var originProperty) && originProperty.ValueKind == JsonValueKind.String)
                {
                    originId = originProperty.GetString();
                }

                long sequence = 0;
                if (root.TryGetProperty("sequence", out var seqProperty) && seqProperty.ValueKind == JsonValueKind.Number)
                {
                    sequence = seqProperty.GetInt64();
                }

                if (!root.TryGetProperty("diff", out var diffProperty) || diffProperty.ValueKind != JsonValueKind.Object)
                    return;

                var diff = ReadDiffDictionary(diffProperty);
                OnStateDiff?.Invoke(role, originId, sequence, diff);
            }
            catch (JsonException)
            {
                // mensaje no compatible
            }
        }

        private void TryEmitCommand(string json)
        {
            try
            {
                using var document = JsonDocument.Parse(json);
                var root = document.RootElement;
                if (!root.TryGetProperty("type", out var typeProperty) ||
                    !string.Equals(typeProperty.GetString(), "command", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                string? eventName = null;
                if (root.TryGetProperty("eventName", out var eventNameProp) && eventNameProp.ValueKind == JsonValueKind.String)
                    eventName = eventNameProp.GetString();
                if (string.IsNullOrWhiteSpace(eventName) && root.TryGetProperty("event", out var eventProp) && eventProp.ValueKind == JsonValueKind.String)
                    eventName = eventProp.GetString();
                if (string.IsNullOrWhiteSpace(eventName))
                    return;

                string? originId = null;
                if (root.TryGetProperty("originId", out var originProp) && originProp.ValueKind == JsonValueKind.String)
                    originId = originProp.GetString();

                long sequence = 0;
                if (root.TryGetProperty("sequence", out var seqProp) && seqProp.ValueKind == JsonValueKind.Number)
                    sequence = seqProp.GetInt64();

                long timestamp = 0;
                if (root.TryGetProperty("timestamp", out var tsProp) && tsProp.ValueKind == JsonValueKind.Number)
                    timestamp = tsProp.GetInt64();

                string? path = null;
                if (root.TryGetProperty("path", out var pathProp) && pathProp.ValueKind == JsonValueKind.String)
                    path = pathProp.GetString();

                object? value = null;
                if (root.TryGetProperty("value", out var valueProp))
                    value = ReadJsonValue(valueProp);
                else if (root.TryGetProperty("data", out var dataProp))
                    value = ReadJsonValue(dataProp);

                OnCommand?.Invoke(new CommandPayload(eventName!, originId, sequence, timestamp, path, value));
            }
            catch (JsonException)
            {
                // mensaje no compatible
            }
        }

        private static Dictionary<string, object?> ReadDiffDictionary(JsonElement element)
        {
            var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in element.EnumerateObject())
            {
                result[property.Name] = ReadJsonValue(property.Value);
            }

            return result;
        }

        private static object? ReadJsonValue(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Object => ReadDiffDictionary(element),
                JsonValueKind.Array => ReadArray(element),
                _ => null
            };
        }

        private static List<object?> ReadArray(JsonElement element)
        {
            var list = new List<object?>();
            foreach (var item in element.EnumerateArray())
            {
                list.Add(ReadJsonValue(item));
            }

            return list;
        }

        public void Dispose()
        {
            if (isHost)
            {
                if (host != null)
                {
                    host.OnMessage -= HandleHostMessage;
                    host.Stop();
                }
            }
            else
            {
                try { linkedCts?.Cancel(); } catch { }
                linkedCts?.Dispose();
                clientConnected = false;
                client?.Dispose();
            }

            StopPingLoop();
        }

        private void StartPingLoop(CancellationToken externalToken)
        {
            StopPingLoop();
            pingCts = CancellationTokenSource.CreateLinkedTokenSource(externalToken);
            var token = pingCts.Token;
            _ = Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1), token).ConfigureAwait(false);
                        await SendPingAsync().ConfigureAwait(false);
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[WebSocket] ⚠️ Error enviando ping: {ex.Message}");
                    }
                }
            }, token);
        }

        private void StopPingLoop()
        {
            if (pingCts == null)
                return;

            try { pingCts.Cancel(); } catch { }
            pingCts.Dispose();
            pingCts = null;
        }

        private async Task SendPingAsync()
        {
            if (isHost)
            {
                if (host?.HasClients != true)
                    return;
            }
            else
            {
                if (!IsConnected)
                    return;
            }

            var ticks = DateTime.UtcNow.Ticks;
            await SendInternalAsync("ping", ticks).ConfigureAwait(false);
        }

        private Task SendInternalAsync(string type, long ticks)
        {
            var payload = JsonSerializer.Serialize(new { type, t = ticks });
            return isHost ? BroadcastAsync(payload) : SendAsync(payload);
        }

        private void RegisterRtt(double rtt)
        {
            lock (rttLock)
            {
                rttSamples.Enqueue(rtt);
                while (rttSamples.Count > 5)
                    rttSamples.Dequeue();

                averageRtt = rttSamples.Count == 0 ? 0 : rttSamples.Average();
            }
        }

        private static bool IsInternalPayload(string json)
        {
            try
            {
                using var document = JsonDocument.Parse(json);
                if (!document.RootElement.TryGetProperty("type", out var typeProperty))
                    return false;

                var type = typeProperty.GetString();
                return string.Equals(type, "ping", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(type, "pong", StringComparison.OrdinalIgnoreCase);
            }
            catch (JsonException)
            {
                return false;
            }
        }
    }
}
