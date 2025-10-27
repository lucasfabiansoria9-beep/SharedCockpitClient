using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SharedCockpitClient.Utils;

namespace SharedCockpitClient.Network
{
    /// <summary>
    /// Servidor WebSocket que maneja las conexiones del copiloto.
    /// Retrocompatible con clientes que no envían protocolo.
    /// </summary>
    public class WebSocketHost
    {
        private readonly HttpListener listener;
        private readonly ConcurrentDictionary<Guid, WebSocket> clients = new();
        private readonly CancellationTokenSource cts = new();
        private readonly int port;

        public event Action<Guid>? OnClientConnected;
        public event Action<Guid>? OnClientDisconnected;
        public event Action<string>? OnMessage;

        public bool HasClients => clients.Count > 0;

        public WebSocketHost(int port = 8081)
        {
            this.port = port;
            listener = new HttpListener();
            listener.Prefixes.Add($"http://+:{port}/");
        }

        public void Start() => _ = Task.Run(RunAsync);

        public void Stop()
        {
            try
            {
                cts.Cancel();
                listener.Stop();
                Logger.Info("🛑 Servidor WebSocket detenido.");
            }
            catch (Exception ex)
            {
                Logger.Warn($"⚠️ Error al detener servidor: {ex.Message}");
            }
        }

        private async Task RunAsync()
        {
            listener.Start();
            Logger.Info($"🛰️ Servidor WebSocket activo en ws://0.0.0.0:{port}");

            while (!cts.IsCancellationRequested)
            {
                try
                {
                    var context = await listener.GetContextAsync();

                    if (!context.Request.IsWebSocketRequest)
                    {
                        context.Response.StatusCode = 400;
                        context.Response.Close();
                        continue;
                    }

                    // 🔧 Handshake flexible: aceptar con o sin subprotocolo
                    WebSocketContext? wsContext = null;
                    try
                    {
                        wsContext = await context.AcceptWebSocketAsync(subProtocol: "sharedcockpit");
                        Logger.Info("✅ Cliente aceptado con protocolo sharedcockpit.");
                    }
                    catch (Exception)
                    {
                        try
                        {
                            wsContext = await context.AcceptWebSocketAsync(subProtocol: null);
                            Logger.Warn("⚙️ Cliente aceptado sin protocolo (modo compatibilidad).");
                        }
                        catch (Exception inner)
                        {
                            Logger.Error($"❌ Error irrecuperable aceptando cliente: {inner.Message}");
                            continue;
                        }
                    }

                    var socket = wsContext.WebSocket;
                    var clientId = Guid.NewGuid();
                    clients[clientId] = socket;

                    Logger.Info($"👥 Cliente conectado ({clientId})");
                    OnClientConnected?.Invoke(clientId);

                    await SendStringAsync(socket, "ROLE:COPILOT");
                    _ = Task.Run(() => HandleClientAsync(clientId, socket));
                }
                catch (Exception ex)
                {
                    Logger.Warn($"⚠️ Error aceptando cliente: {ex.Message}");
                }
            }
        }

        private async Task HandleClientAsync(Guid clientId, WebSocket socket)
        {
            var buffer = new byte[8192];

            try
            {
                while (socket.State == WebSocketState.Open && !cts.IsCancellationRequested)
                {
                    var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);

                    if (result.MessageType == WebSocketMessageType.Close)
                        break;

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var msg = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        msg = MaybeDecompress(msg);
                        OnMessage?.Invoke(msg);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"⚠️ Error en cliente ({clientId}): {ex.Message}");
            }
            finally
            {
                if (clients.TryRemove(clientId, out var ws))
                {
                    try { await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Desconectado", CancellationToken.None); } catch { }
                    ws.Dispose();
                    Logger.Info($"👋 Cliente desconectado ({clientId})");
                    OnClientDisconnected?.Invoke(clientId);
                }
            }
        }

        public void Broadcast(string message)
        {
            foreach (var kv in clients)
            {
                var socket = kv.Value;
                if (socket.State == WebSocketState.Open)
                {
                    try
                    {
                        _ = SendStringAsync(socket, message);
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn($"⚠️ Error enviando mensaje a {kv.Key}: {ex.Message}");
                    }
                }
            }
        }

        public void SendToClient(Guid clientId, string message)
        {
            if (!clients.TryGetValue(clientId, out var socket))
                return;

            if (socket.State != WebSocketState.Open)
                return;

            _ = SendStringAsync(socket, message);
        }

        private static async Task SendStringAsync(WebSocket socket, string message)
        {
            var prepared = PreparePayload(message);
            var data = Encoding.UTF8.GetBytes(prepared);
            await socket.SendAsync(new ArraySegment<byte>(data),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None);
        }

        public void ReservePilotRole() => Logger.Info("🧭 Rol del servidor: PILOT");

        private static string PreparePayload(string message)
        {
            var bytes = Encoding.UTF8.GetBytes(message);
            if (bytes.Length < 512)
                return message;

            try
            {
                using var output = new MemoryStream();
                using (var gzip = new GZipStream(output, CompressionLevel.Fastest, leaveOpen: true))
                {
                    gzip.Write(bytes, 0, bytes.Length);
                }
                return "gz:" + Convert.ToBase64String(output.ToArray());
            }
            catch
            {
                return message;
            }
        }

        private static string MaybeDecompress(string message)
        {
            if (!message.StartsWith("gz:", StringComparison.Ordinal))
                return message;

            try
            {
                var compressed = Convert.FromBase64String(message[3..]);
                using var input = new MemoryStream(compressed);
                using var gzip = new GZipStream(input, CompressionMode.Decompress);
                using var reader = new StreamReader(gzip, Encoding.UTF8);
                return reader.ReadToEnd();
            }
            catch
            {
                return message;
            }
        }
    }
}
