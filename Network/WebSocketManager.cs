using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

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
        private readonly WebSocketHost? host;
        private ClientWebSocket? client;
        private CancellationTokenSource? linkedCts;
        private bool clientConnected;

        public event Action<string>? OnMessage;
        public event Action<string?, Dictionary<string, object?>>? OnStateDiff;

        public WebSocketManager(bool isHost, Uri? peer = null, int? portOverride = null)
        {
            this.isHost = isHost;
            peerUri = peer;

            if (isHost)
            {
                var listenPort = portOverride ?? peer?.Port ?? 8081;
                host = new WebSocketHost(listenPort);
                host.OnMessage += HandleHostMessage;
                host.OnClientConnected += id => Console.WriteLine($"[WebSocket] Cliente conectado ({id})");
                host.OnClientDisconnected += id => Console.WriteLine($"[WebSocket] Cliente desconectado ({id})");
                host.Start();
                Console.WriteLine($"[WebSocket] Host escuchando en ws://0.0.0.0:{listenPort}");
            }
        }

        public bool IsConnected => isHost
            ? host?.HasClients == true
            : clientConnected && client?.State == WebSocketState.Open;

        public async Task StartAsync(CancellationToken ct)
        {
            if (isHost)
            {
                await Task.CompletedTask;
                return;
            }

            linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            client = new ClientWebSocket();
            await client.ConnectAsync(peerUri ?? throw new InvalidOperationException("Peer no especificado para modo cliente."), ct)
                .ConfigureAwait(false);
            Console.WriteLine($"[WebSocket] Conectado a {peerUri}");
            clientConnected = true;
            _ = Task.Run(() => ReceiveLoopAsync(client, linkedCts.Token));
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
            Console.WriteLine("[Broadcast] Rebroadcast a clientes (serverTime sellado)");
            await Task.CompletedTask;
        }

        private void HandleHostMessage(string json)
        {
            if (TryStampServerTime(json, out var stamped))
            {
                json = stamped;
            }

            host?.Broadcast(json);
            Console.WriteLine("[Broadcast] Rebroadcast a clientes (serverTime sellado)");
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
            OnMessage?.Invoke(json);
            TryEmitStateDiff(json);
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

                if (!root.TryGetProperty("diff", out var diffProperty) || diffProperty.ValueKind != JsonValueKind.Object)
                    return;

                var diff = ReadDiffDictionary(diffProperty);
                OnStateDiff?.Invoke(role, diff);
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
        }
    }
}
