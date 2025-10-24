using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SharedCockpitClient.Utils;

namespace SharedCockpitClient.Network
{
    /// <summary>
    /// Servidor WebSocket compatible con .NET 8.
    /// No requiere permisos de administrador y acepta conexiones del copiloto.
    /// </summary>
    public sealed class WebSocketHost : IDisposable
    {
        private readonly TcpListener listener;
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
            listener = new TcpListener(IPAddress.Any, port);
        }

        public void Start()
        {
            if (disposed || IsRunning) return;

            try
            {
                listener.Start();
                cts = new CancellationTokenSource();
                listenTask = Task.Run(() => AcceptLoopAsync(cts.Token));

                Logger.Info($"🛰️ Servidor WebSocket activo en ws://0.0.0.0:{port}");
            }
            catch (Exception ex)
            {
                Logger.Error($"⚠️ No se pudo iniciar el servidor WebSocket: {ex.Message}");
                Stop();
            }
        }

        public void Stop()
        {
            if (disposed) return;

            try
            {
                cts?.Cancel();
                listener.Stop();
            }
            catch (Exception ex)
            {
                Logger.Warn($"⚠️ Error al detener servidor WebSocket: {ex.Message}");
            }

            foreach (var kvp in clients)
            {
                try
                {
                    if (kvp.Value.State is WebSocketState.Open or WebSocketState.CloseReceived)
                    {
                        kvp.Value.CloseAsync(WebSocketCloseStatus.NormalClosure, "Servidor detenido", CancellationToken.None)
                            .Wait(TimeSpan.FromSeconds(1));
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

            Logger.Info("🛑 Servidor WebSocket detenido.");
        }

        public bool IsRunning => listener.Server.IsBound;
        public bool HasClients => !clients.IsEmpty;

        public void Broadcast(string message)
        {
            if (!IsRunning || string.IsNullOrEmpty(message))
                return;

            var buffer = Encoding.UTF8.GetBytes(message);
            foreach (var (id, socket) in clients)
            {
                if (socket.State == WebSocketState.Open)
                {
                    _ = SendSafeAsync(id, socket, buffer);
                }
            }
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            Stop();
        }

        private async Task AcceptLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                TcpClient? tcpClient = null;

                try
                {
                    tcpClient = await listener.AcceptTcpClientAsync(token).ConfigureAwait(false);
                    _ = HandleClientAsync(tcpClient, token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Logger.Error($"⚠️ Error aceptando conexión TCP: {ex.Message}");
                    tcpClient?.Close();
                }
            }
        }

        private async Task HandleClientAsync(TcpClient tcpClient, CancellationToken token)
        {
            var clientId = Guid.NewGuid();
            Logger.Info($"👥 Cliente conectado ({clientId})");

            try
            {
                using var stream = tcpClient.GetStream();
                // 🔧 Fix: parámetro corregido para .NET 8
                var socket = WebSocket.CreateFromStream(
                    stream,
                    isServer: true,
                    subProtocol: null,
                    keepAliveInterval: TimeSpan.FromMinutes(2)
                );

                clients[clientId] = socket;

                // Asignar rol según orden de conexión
                var role = clients.Count == 1 ? "PILOT" : "COPILOT";
                Logger.Info($"🧭 Rol asignado: {role}");
                var roleMsg = Encoding.UTF8.GetBytes($"ROLE:{role}");
                await socket.SendAsync(new ArraySegment<byte>(roleMsg), WebSocketMessageType.Text, true, token);

                OnClientConnected.Invoke();
                await ReceiveLoopAsync(clientId, socket, token);
            }
            catch (Exception ex)
            {
                Logger.Error($"⚠️ Error en cliente ({clientId}): {ex.Message}");
            }
            finally
            {
                if (clients.TryRemove(clientId, out var socket))
                {
                    if (socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
                    {
                        try
                        {
                            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Cierre normal", CancellationToken.None);
                        }
                        catch
                        {
                            socket.Abort();
                        }
                    }

                    socket.Dispose();
                }

                tcpClient.Close();
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
                        result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), token);
                        if (result.MessageType == WebSocketMessageType.Close)
                            return;

                        if (result.MessageType == WebSocketMessageType.Text)
                            builder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));

                    } while (!result.EndOfMessage);

                    if (builder.Length > 0)
                        OnMessage.Invoke(builder.ToString());
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Logger.Warn($"⚠️ Error en cliente ({clientId}): {ex.Message}");
            }
        }

        private async Task SendSafeAsync(Guid clientId, WebSocket socket, byte[] buffer)
        {
            try
            {
                await socket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
            }
            catch (Exception ex)
            {
                Logger.Warn($"⚠️ Error enviando mensaje a cliente ({clientId}): {ex.Message}");
            }
        }
    }
}
