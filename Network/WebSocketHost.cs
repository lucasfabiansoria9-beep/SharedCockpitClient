using System;
using System.Collections.Concurrent;
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
    /// Retrocompatible con el controlador SyncController.
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

        public void Start()
        {
            _ = Task.Run(RunAsync);
        }

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

                    // 🔧 Sin compresión: handshake limpio
                    var wsContext = await context.AcceptWebSocketAsync(subProtocol: "sharedcockpit");
                    var socket = wsContext.WebSocket;

                    var clientId = Guid.NewGuid();
                    clients[clientId] = socket;

                    Logger.Info($"👥 Cliente conectado ({clientId})");
                    OnClientConnected?.Invoke(clientId);

                    await SendAsync(socket, "ROLE:COPILOT");
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
            var payload = Encoding.UTF8.GetBytes(message);

            foreach (var kv in clients)
            {
                var socket = kv.Value;
                if (socket.State == WebSocketState.Open)
                {
                    try
                    {
                        _ = socket.SendAsync(
                            new ArraySegment<byte>(payload),
                            WebSocketMessageType.Text,
                            true,
                            CancellationToken.None
                        );
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn($"⚠️ Error enviando mensaje a {kv.Key}: {ex.Message}");
                    }
                }
            }
        }

        private static async Task SendAsync(WebSocket socket, string message)
        {
            var data = Encoding.UTF8.GetBytes(message);
            await socket.SendAsync(new ArraySegment<byte>(data),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None);
        }

        public void ReservePilotRole() => Logger.Info("🧭 Rol del servidor: PILOT");
    }
}
