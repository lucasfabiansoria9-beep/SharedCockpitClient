using System;
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

        public event Action<string>? OnMessage;

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
            OnMessage?.Invoke(json);
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
                        return;
                    if (result.MessageType != WebSocketMessageType.Text)
                        continue;

                    builder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                }
                while (!result.EndOfMessage);

                if (builder.Length > 0)
                {
                    var message = MaybeDecompress(builder.ToString());
                    OnMessage?.Invoke(message);
                }
            }
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
                client?.Dispose();
            }
        }
    }
}
