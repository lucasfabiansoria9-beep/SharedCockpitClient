using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SharedCockpitClient.Utils;

namespace SharedCockpitClient.Network
{
    /// <summary>
    /// Servidor WebSocket que acepta conexiones sin depender de HttpListener.
    /// Gestiona los roles de los clientes (piloto/copiloto), permite broadcast
    /// y expone eventos para conexiones, desconexiones y mensajes recibidos.
    /// </summary>
    public sealed class WebSocketHost : IDisposable
    {
        private readonly TcpListener listener;
        private readonly ConcurrentDictionary<Guid, ClientConnection> clients = new();
        private readonly object roleLock = new();
        private readonly int port;

        private CancellationTokenSource? cts;
        private Task? listenTask;
        private bool disposed;
        private bool pilotRoleReserved;
        private Guid? pilotClientId;
        private Guid? copilotClientId;

        public event Action OnClientConnected = delegate { };
        public event Action OnClientDisconnected = delegate { };
        public event Action<string> OnMessage = delegate { };

        public WebSocketHost(int port)
        {
            this.port = port;
            listener = new TcpListener(IPAddress.Any, port);
        }

        /// <summary>
        /// Permite reservar el rol de piloto para el proceso host sin consumir una conexión WebSocket.
        /// </summary>
        public void ReservePilotRole()
        {
            lock (roleLock)
            {
                pilotRoleReserved = true;
            }
        }

        public void Start()
        {
            if (disposed || cts != null)
                return;

            cts = new CancellationTokenSource();

            try
            {
                listener.Start();
                Logger.Info($"🛰️ Servidor WebSocket activo en ws://0.0.0.0:{port}");
                listenTask = Task.Run(() => AcceptLoopAsync(cts.Token));
            }
            catch (SocketException ex)
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
                // Ignorar: la fuente ya fue liberada.
            }

            try
            {
                listener.Stop();
            }
            catch (SocketException ex)
            {
                Logger.Warn($"⚠️ Error al detener el servidor WebSocket: {ex.Message}");
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

                listenTask = null;
            }

            foreach (var kvp in clients.ToArray())
            {
                var connection = kvp.Value;

                try
                {
                    if (connection.Socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
                    {
                        connection.Socket.CloseAsync(
                            WebSocketCloseStatus.NormalClosure,
                            "Servidor detenido",
                            CancellationToken.None
                        ).Wait(TimeSpan.FromSeconds(1));
                    }
                    else
                    {
                        connection.Socket.Abort();
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warn($"⚠️ Error al cerrar cliente WebSocket ({connection.Role}, {kvp.Key}): {ex.Message}");
                }
            }

            cts?.Dispose();
            cts = null;

            Logger.Info("🛑 Servidor WebSocket detenido.");
        }

        public bool HasClients => !clients.IsEmpty;

        public void Broadcast(string message)
        {
            if (string.IsNullOrEmpty(message))
                return;

            var buffer = Encoding.UTF8.GetBytes(message);
            foreach (var (id, connection) in clients)
            {
                if (connection.Socket.State != WebSocketState.Open)
                    continue;

                _ = SendSafeAsync(id, connection.Socket, buffer);
            }
        }

        public void Dispose()
        {
            if (disposed)
                return;

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
                    tcpClient.NoDelay = true;
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Logger.Error($"⚠️ Error aceptando conexión WebSocket: {ex.Message}");
                    tcpClient?.Dispose();
                    continue;
                }

                _ = HandleClientAsync(tcpClient, token);
            }
        }

        private async Task HandleClientAsync(TcpClient tcpClient, CancellationToken token)
        {
            var clientId = Guid.NewGuid();
            ClientConnection? connection = null;
            bool registered = false;

            try
            {
                var socket = await AcceptWebSocketAsync(tcpClient, token).ConfigureAwait(false);
                if (socket == null)
                {
                    tcpClient.Dispose();
                    return;
                }

                var role = AssignRole(clientId);
                connection = new ClientConnection(tcpClient, socket, role);

                if (!clients.TryAdd(clientId, connection))
                {
                    Logger.Warn("⚠️ No se pudo registrar el cliente WebSocket.");
                    await CloseSocketAsync(socket).ConfigureAwait(false);
                    connection.Dispose();
                    ReleaseRole(clientId, role);
                    return;
                }

                registered = true;

                if (role.Equals("COPILOT", StringComparison.OrdinalIgnoreCase))
                {
                    Logger.Info("👥 Copiloto conectado al servidor WebSocket.");
                }
                else
                {
                    Logger.Info("🧑‍✈️ Piloto principal conectado al servidor WebSocket.");
                }

                OnClientConnected.Invoke();

                await SendRoleAsync(socket, role, token).ConfigureAwait(false);

                await ReceiveLoopAsync(clientId, connection, token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Cancelado durante la detención del servidor.
            }
            catch (Exception ex)
            {
                Logger.Error($"⚠️ Excepción manejando cliente WebSocket ({clientId}): {ex.Message}");
            }
            finally
            {
                if (registered)
                {
                    clients.TryRemove(clientId, out var removedConnection);
                    var effectiveConnection = removedConnection ?? connection;

                    if (effectiveConnection != null)
                    {
                        ReleaseRole(clientId, effectiveConnection.Role);
                        await CloseSocketAsync(effectiveConnection.Socket).ConfigureAwait(false);
                        effectiveConnection.Dispose();

                        if (effectiveConnection.Role.Equals("COPILOT", StringComparison.OrdinalIgnoreCase))
                        {
                            Logger.Info("👋 Copiloto desconectado del servidor WebSocket.");
                        }
                        else
                        {
                            Logger.Info("👋 Piloto desconectado del servidor WebSocket.");
                        }
                    }

                    OnClientDisconnected.Invoke();
                }
                else
                {
                    tcpClient.Dispose();
                }
            }
        }

        private string AssignRole(Guid clientId)
        {
            lock (roleLock)
            {
                if (!pilotRoleReserved && pilotClientId == null)
                {
                    pilotClientId = clientId;
                    return "PILOT";
                }

                if (copilotClientId == null)
                {
                    copilotClientId = clientId;
                    return "COPILOT";
                }

                return "COPILOT";
            }
        }

        private void ReleaseRole(Guid clientId, string? role)
        {
            if (string.IsNullOrEmpty(role))
                return;

            lock (roleLock)
            {
                if (role.Equals("PILOT", StringComparison.OrdinalIgnoreCase) && pilotClientId == clientId)
                {
                    pilotClientId = null;
                }
                else if (role.Equals("COPILOT", StringComparison.OrdinalIgnoreCase) && copilotClientId == clientId)
                {
                    copilotClientId = null;
                }
            }
        }

        private async Task ReceiveLoopAsync(Guid clientId, ClientConnection connection, CancellationToken token)
        {
            var buffer = new byte[8192];
            var socket = connection.Socket;

            try
            {
                while (!token.IsCancellationRequested && socket.State == WebSocketState.Open)
                {
                    var builder = new StringBuilder();
                    WebSocketReceiveResult result;

                    do
                    {
                        result = await socket.ReceiveAsync(buffer.AsMemory(0, buffer.Length), token).ConfigureAwait(false);

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
                Logger.Warn($"⚠️ Error en cliente WebSocket ({connection.Role}, {clientId}): {ex.Message}");
            }
            catch (Exception ex)
            {
                Logger.Error($"⚠️ Excepción en recepción WebSocket ({connection.Role}, {clientId}): {ex.Message}");
            }
        }

        private static async Task SendRoleAsync(WebSocket socket, string role, CancellationToken token)
        {
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
                Logger.Warn($"⚠️ No se pudo enviar rol al cliente: {ex.Message}");
            }
        }

        private static async Task CloseSocketAsync(WebSocket socket)
        {
            try
            {
                if (socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
                {
                    await socket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Cierre solicitado",
                        CancellationToken.None
                    ).ConfigureAwait(false);
                }
            }
            catch (ObjectDisposedException)
            {
                // Ya fue cerrado.
            }
            catch
            {
                try
                {
                    socket.Abort();
                }
                catch
                {
                    // Ignorar.
                }
            }
        }

        private async Task<WebSocket?> AcceptWebSocketAsync(TcpClient tcpClient, CancellationToken token)
        {
            var stream = tcpClient.GetStream();
            var request = await ReadHttpRequestAsync(stream, token).ConfigureAwait(false);
            if (request == null)
            {
                await SendHttpErrorAsync(stream, token).ConfigureAwait(false);
                return null;
            }

            var (requestLine, headers) = request.Value;
            if (!requestLine.StartsWith("GET", StringComparison.OrdinalIgnoreCase))
            {
                await SendHttpErrorAsync(stream, token).ConfigureAwait(false);
                return null;
            }

            if (!headers.TryGetValue("Sec-WebSocket-Key", out var websocketKey) || string.IsNullOrWhiteSpace(websocketKey))
            {
                await SendHttpErrorAsync(stream, token).ConfigureAwait(false);
                return null;
            }

            if (!headers.TryGetValue("Upgrade", out var upgrade) ||
                !upgrade.Equals("websocket", StringComparison.OrdinalIgnoreCase))
            {
                await SendHttpErrorAsync(stream, token).ConfigureAwait(false);
                return null;
            }

            if (!headers.TryGetValue("Connection", out var connectionHeader) ||
                !connectionHeader.Contains("Upgrade", StringComparison.OrdinalIgnoreCase))
            {
                await SendHttpErrorAsync(stream, token).ConfigureAwait(false);
                return null;
            }

            if (headers.TryGetValue("Sec-WebSocket-Version", out var version) && !string.Equals(version, "13", StringComparison.OrdinalIgnoreCase))
            {
                await SendHttpErrorAsync(stream, token).ConfigureAwait(false);
                return null;
            }

            var acceptKey = Convert.ToBase64String(
                SHA1.HashData(Encoding.ASCII.GetBytes(websocketKey.Trim() + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11"))
            );

            var response = new StringBuilder();
            response.AppendLine("HTTP/1.1 101 Switching Protocols");
            response.AppendLine("Upgrade: websocket");
            response.AppendLine("Connection: Upgrade");
            response.Append("Sec-WebSocket-Accept: ");
            response.AppendLine(acceptKey);
            response.AppendLine();

            var responseBytes = Encoding.ASCII.GetBytes(response.ToString());
            await stream.WriteAsync(responseBytes.AsMemory(0, responseBytes.Length), token).ConfigureAwait(false);

            return await WebSocket.CreateFromStreamAsync(
                stream,
                isServer: true,
                subProtocol: null,
                keepAliveInterval: TimeSpan.FromSeconds(60),
                cancellationToken: token
            ).ConfigureAwait(false);
        }

        private static async Task<(string RequestLine, Dictionary<string, string> Headers)?> ReadHttpRequestAsync(NetworkStream stream, CancellationToken token)
        {
            var buffer = new byte[4096];
            var builder = new StringBuilder();

            while (true)
            {
                var bytesRead = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), token).ConfigureAwait(false);
                if (bytesRead == 0)
                    return null;

                builder.Append(Encoding.ASCII.GetString(buffer, 0, bytesRead));

                if (builder.ToString().Contains("\r\n\r\n", StringComparison.Ordinal))
                    break;

                if (builder.Length > 16_384)
                    return null;
            }

            var requestText = builder.ToString();
            var headerEndIndex = requestText.IndexOf("\r\n\r\n", StringComparison.Ordinal);
            if (headerEndIndex < 0)
                return null;

            var headerLines = requestText[..headerEndIndex].Split(new[] { "\r\n" }, StringSplitOptions.None);
            if (headerLines.Length == 0)
                return null;

            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 1; i < headerLines.Length; i++)
            {
                var line = headerLines[i];
                var separatorIndex = line.IndexOf(':');
                if (separatorIndex <= 0)
                    continue;

                var name = line[..separatorIndex].Trim();
                var value = line[(separatorIndex + 1)..].Trim();
                headers[name] = value;
            }

            return (headerLines[0], headers);
        }

        private static async Task SendHttpErrorAsync(NetworkStream stream, CancellationToken token)
        {
            try
            {
                var responseBytes = Encoding.ASCII.GetBytes("HTTP/1.1 400 Bad Request\r\nConnection: close\r\nContent-Length: 0\r\n\r\n");
                await stream.WriteAsync(responseBytes.AsMemory(0, responseBytes.Length), token).ConfigureAwait(false);
            }
            catch
            {
                // Ignorar: estamos cerrando la conexión igualmente.
            }
        }

        private sealed class ClientConnection : IDisposable
        {
            public ClientConnection(TcpClient tcpClient, WebSocket socket, string role)
            {
                TcpClient = tcpClient;
                Socket = socket;
                Role = role;
            }

            public TcpClient TcpClient { get; }

            public WebSocket Socket { get; }

            public string Role { get; }

            public void Dispose()
            {
                try
                {
                    Socket.Dispose();
                }
                catch
                {
                    // Ignorar errores al disponer.
                }

                try
                {
                    TcpClient.Dispose();
                }
                catch
                {
                    // Ignorar errores al disponer.
                }
            }
        }
    }
}
