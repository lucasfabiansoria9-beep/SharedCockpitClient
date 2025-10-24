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
    /// Servidor WebSocket sencillo que acepta conexiones entrantes del copiloto.
    /// Permite enviar mensajes a todos los clientes conectados y expone eventos
    /// para reaccionar a la conexión, desconexión y mensajes recibidos.
    /// </summary>
    public sealed class WebSocketHost : IDisposable
    {
        private readonly HttpListener listener;
        private readonly ConcurrentDictionary<Guid, WebSocket> clients = new();
        private readonly int port;
        private CancellationTokenSource? cts;
        private Task? listenTask;
        private bool disposed;

        public event Action OnClientConnected = delegate { };
        public event Action OnClientDisconnected = delegate { };
        public event Action<string> OnMessage = delegate { };

        public WebSocketHost(int port)
        {
            this.port = port;
            listener = new HttpListener();
            listener.Prefixes.Add($"http://0.0.0.0:{port}/");
        }

        public void Start()
        {
            if (listener.IsListening || disposed)
                return;

            cts = new CancellationTokenSource();

            try
            {
                listener.Start();
                Logger.Info($"🛰️ Servidor WebSocket activo en ws://0.0.0.0:{port}");
                listenTask = Task.Run(() => AcceptLoopAsync(cts.Token));
            }
            catch (HttpListenerException ex)
            {
                Logger.Error($"⚠️ No se pudo iniciar el servidor WebSocket: {ex.Message}");
                Stop();
            }
        }

        public void Stop()
        {
            if (disposed)
                return;

            try
            {
                cts?.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // Ignorar: el token ya fue liberado.
            }

            if (listener.IsListening)
            {
                try
                {
                    listener.Stop();
                }
                catch (ObjectDisposedException)
                {
                    // Ya está detenido.
                }
            }

            if (listenTask != null)
            {
                try
                {
                    listenTask.Wait(TimeSpan.FromSeconds(2));
                }
                catch (AggregateException ex)
                {
                    Logger.Warn($"⚠️ Error al esperar la tarea del servidor WebSocket: {ex.Flatten().InnerException?.Message}");
                }
            }

            foreach (var kvp in clients)
            {
                try
                {
                    if (kvp.Value.State is WebSocketState.Open or WebSocketState.CloseReceived)
                    {
                        kvp.Value.CloseAsync(
                            WebSocketCloseStatus.NormalClosure,
                            "Servidor detenido",
                            CancellationToken.None
                        ).Wait(TimeSpan.FromSeconds(1));
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warn($"⚠️ Error al cerrar cliente WebSocket: {ex.Message}");
                }
                finally
                {
                    kvp.Value.Abort();
                    kvp.Value.Dispose();
                }
            }

            clients.Clear();
            cts?.Dispose();
            cts = null;
            listenTask = null;

            Logger.Info("🛑 Servidor WebSocket detenido.");
        }

        public bool HasClients => !clients.IsEmpty;

        public void Broadcast(string message)
        {
            if (!listener.IsListening || string.IsNullOrEmpty(message))
                return;

            var buffer = Encoding.UTF8.GetBytes(message);
            foreach (var (id, socket) in clients)
            {
                if (socket.State != WebSocketState.Open)
                    continue;

                _ = SendSafeAsync(id, socket, buffer);
            }
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            Stop();
            listener.Close();
        }

        private async Task AcceptLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                HttpListenerContext? context = null;

                try
                {
                    context = await listener.GetContextAsync().ConfigureAwait(false);
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (HttpListenerException ex) when (token.IsCancellationRequested)
                {
                    Logger.Warn($"⚠️ Ciclo de aceptación detenido: {ex.Message}");
                    break;
                }
                catch (Exception ex)
                {
                    Logger.Error($"⚠️ Error aceptando conexión WebSocket: {ex.Message}");
                    continue;
                }

                if (context == null)
                    continue;

                if (!context.Request.IsWebSocketRequest)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    context.Response.Close();
                    continue;
                }

                _ = HandleClientAsync(context, token);
            }
        }

        private async Task HandleClientAsync(HttpListenerContext context, CancellationToken token)
        {
            HttpListenerWebSocketContext? wsContext = null;
            try
            {
                wsContext = await context.AcceptWebSocketAsync(subProtocol: null).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Error($"⚠️ Error al aceptar WebSocket: {ex.Message}");
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                context.Response.Close();
                return;
            }

            var socket = wsContext.WebSocket;
            var clientId = Guid.NewGuid();

            // 🧩 Asignación automática de rol: el primer cliente es PILOT, el siguiente es COPILOT
            var role = clients.IsEmpty ? "PILOT" : "COPILOT";
            clients[clientId] = socket;

            Logger.Info($"👥 Cliente conectado ({clientId}) asignado como {role}");
            OnClientConnected.Invoke();

            // Enviar rol al cliente al conectarse
            try
            {
                var payload = Encoding.UTF8.GetBytes($"ROLE:{role}");
                await socket.SendAsync(
                    new ArraySegment<byte>(payload),
                    WebSocketMessageType.Text,
                    true,
                    token
                ).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Warn($"⚠️ No se pudo enviar rol al cliente {clientId}: {ex.Message}");
            }

            try
            {
                await ReceiveLoopAsync(clientId, socket, token).ConfigureAwait(false);
            }
            finally
            {
                clients.TryRemove(clientId, out _);

                if (socket.State != WebSocketState.Closed && socket.State != WebSocketState.Aborted)
                {
                    try
                    {
                        await socket.CloseAsync(
                            WebSocketCloseStatus.NormalClosure,
                            "Cierre solicitado",
                            CancellationToken.None
                        ).ConfigureAwait(false);
                    }
                    catch
                    {
                        socket.Abort();
                    }
                }

                socket.Dispose();
                Logger.Info($"👋 Cliente desconectado ({clientId})");
                OnClientDisconnected.Invoke();
            }
        }

        private async Task ReceiveLoopAsync(Guid clientId, WebSocket socket, CancellationToken token)
        {
            var buffer = new byte[8192];

            try
            {
                while (!token.IsCancellationRequested && socket.State == WebSocketState.Open)
                {
                    var builder = new StringBuilder();
                    WebSocketReceiveResult result;
                    do
                    {
                        result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), token).ConfigureAwait(false);

                        if (result.MessageType == WebSocketMessageType.Close)
                            return;

                        if (result.MessageType != WebSocketMessageType.Text)
                            continue;

                        builder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                    }
                    while (!result.EndOfMessage);

                    if (builder.Length > 0)
                        OnMessage.Invoke(builder.ToString());
                }
            }
            catch (OperationCanceledException)
            {
                // Cancelado.
            }
            catch (WebSocketException ex)
            {
                Logger.Warn($"⚠️ Error en cliente WebSocket ({clientId}): {ex.Message}");
            }
            catch (Exception ex)
            {
                Logger.Error($"⚠️ Excepción en recepción WebSocket ({clientId}): {ex.Message}");
            }
        }

        private async Task SendSafeAsync(Guid clientId, WebSocket socket, byte[] buffer)
        {
            try
            {
                await socket.SendAsync(
                    new ArraySegment<byte>(buffer),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None
                ).ConfigureAwait(false);
            }
            catch (WebSocketException ex)
            {
                Logger.Warn($"⚠️ Error enviando a cliente WebSocket ({clientId}): {ex.Message}");
            }
            catch (Exception ex)
            {
                Logger.Error($"⚠️ Excepción enviando a cliente WebSocket ({clientId}): {ex.Message}");
            }
        }
    }
}
