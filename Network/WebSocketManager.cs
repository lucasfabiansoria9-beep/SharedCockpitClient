using System;
using System.IO;
using System.IO.Compression;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SharedCockpitClient.FlightData;
using SharedCockpitClient.Utils;

namespace SharedCockpitClient.Network
{
    /// <summary>
    /// Cliente WebSocket que maneja la conexión entre piloto y copiloto.
    /// Escucha mensajes, maneja reconexiones y notifica eventos al simulador.
    /// </summary>
    public class WebSocketManager : IDisposable
    {
        private readonly string url;
        private readonly SimConnectManager sim;
        private ClientWebSocket? client;
        private CancellationTokenSource? cts;
        private Task? workerTask;
        private bool disposed;
        private string userRole = string.Empty;

        public event Action OnOpen = delegate { };
        public event Action<string> OnMessage = delegate { };
        public event Action<string> OnError = delegate { };
        public event Action OnClose = delegate { };

        public WebSocketManager(string serverUrl, SimConnectManager simConnectManager)
        {
            url = serverUrl ?? throw new ArgumentNullException(nameof(serverUrl));
            sim = simConnectManager ?? throw new ArgumentNullException(nameof(simConnectManager));
        }

        public void Connect()
        {
            if (disposed)
                return;

            if (workerTask != null && !workerTask.IsCompleted)
            {
                Logger.Warn("⚠️ La conexión WebSocket ya está en curso.");
                return;
            }

            cts = new CancellationTokenSource();
            workerTask = Task.Run(() => RunAsync(cts.Token));
        }

        public void Send(string message)
        {
            var socket = client;
            if (socket == null || socket.State != WebSocketState.Open)
            {
                Logger.Warn("⚠️ No se puede enviar el mensaje: WebSocket no está conectado o es nulo.");
                return;
            }

            var payload = PreparePayload(message);
            _ = SendInternalAsync(socket, payload);
        }

        public void Close()
        {
            if (disposed)
                return;

            disposed = true;

            try { cts?.Cancel(); } catch { }

            if (client != null)
            {
                try
                {
                    if (client.State is WebSocketState.Open or WebSocketState.CloseReceived)
                    {
                        client.CloseAsync(
                            WebSocketCloseStatus.NormalClosure,
                            "Cierre solicitado",
                            CancellationToken.None
                        ).Wait(TimeSpan.FromSeconds(1));
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warn($"⚠️ Error al cerrar WebSocket: {ex.Message}");
                }
                finally
                {
                    client.Dispose();
                }
            }

            try { workerTask?.Wait(TimeSpan.FromSeconds(2)); } catch { }

            cts?.Dispose();
            client = null;
            workerTask = null;

            OnClose.Invoke();
            Logger.Info("🔌 Conexión WebSocket cerrada correctamente.");
        }

        public void Dispose() => Close();

        private async Task RunAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                using var ws = new ClientWebSocket();

                // 🔧 Desactiva completamente la compresión para compatibilidad universal
                try
                {
                    ws.Options.SetRequestHeader("Sec-WebSocket-Extensions", ""); // Elimina permessage-deflate
                }
                catch { /* En algunos sistemas SetRequestHeader puede fallar, se ignora */ }

                ws.Options.DangerousDeflateOptions = null; // Evita frames comprimidos
                ws.Options.AddSubProtocol("sharedcockpit"); // Handshake limpio
                ws.Options.KeepAliveInterval = TimeSpan.FromSeconds(20);

                client = ws;

                try
                {
                    await ws.ConnectAsync(new Uri(url), token).ConfigureAwait(false);
                    Logger.Info($"🌐 Conectado al servidor WebSocket: {url}");
                    OnOpen.Invoke();

                    await ReceiveLoopAsync(ws, token).ConfigureAwait(false);

                    if (ws.State == WebSocketState.CloseReceived)
                    {
                        await ws.CloseOutputAsync(
                            WebSocketCloseStatus.NormalClosure,
                            "Cierre reconocido",
                            CancellationToken.None
                        ).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Logger.Error($"❌ Error al conectar WebSocket: {ex.Message}");
                    OnError.Invoke(ex.Message);
                }

                client = null;

                if (token.IsCancellationRequested)
                    break;

                if (ws.State != WebSocketState.Open && ws.State != WebSocketState.Connecting)
                    OnClose.Invoke();

                try { await Task.Delay(TimeSpan.FromSeconds(2), token).ConfigureAwait(false); }
                catch (OperationCanceledException) { break; }

                Logger.Warn("🔁 Intentando reconectar al servidor WebSocket...");
            }
        }

        private async Task ReceiveLoopAsync(ClientWebSocket ws, CancellationToken token)
        {
            var buffer = new byte[8192];

            try
            {
                while (!token.IsCancellationRequested && ws.State == WebSocketState.Open)
                {
                    var builder = new StringBuilder();
                    WebSocketReceiveResult result;
                    do
                    {
                        result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), token).ConfigureAwait(false);

                        if (result.MessageType == WebSocketMessageType.Close)
                            return;

                        if (result.MessageType != WebSocketMessageType.Text)
                            continue;

                        builder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                    }
                    while (!result.EndOfMessage);

                    if (builder.Length > 0)
                        HandleIncomingMessage(builder.ToString());
                }
            }
            catch (OperationCanceledException)
            {
                // Cancelado
            }
            catch (WebSocketException ex)
            {
                Logger.Warn($"⚠️ Error en WebSocket de cliente: {ex.Message}");
                OnError.Invoke(ex.Message);
            }
            catch (Exception ex)
            {
                Logger.Error($"⚠️ Excepción en recepción WebSocket: {ex.Message}");
                OnError.Invoke(ex.Message);
            }
        }

        private async Task SendInternalAsync(ClientWebSocket socket, byte[] payload)
        {
            try
            {
                await socket.SendAsync(
                    new ArraySegment<byte>(payload),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None
                ).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Warn($"⚠️ Error enviando mensaje WebSocket: {ex.Message}");
                OnError.Invoke(ex.Message);
            }
        }

        private void HandleIncomingMessage(string message)
        {
            message = MaybeDecompress(message);
            if (message.StartsWith("ROLE:", StringComparison.OrdinalIgnoreCase))
            {
                var assignedRole = message.Substring("ROLE:".Length).Trim();
                if (!string.IsNullOrEmpty(assignedRole))
                {
                    var normalizedRole = assignedRole.ToUpperInvariant();
                    if (!string.Equals(userRole, normalizedRole, StringComparison.OrdinalIgnoreCase))
                    {
                        userRole = normalizedRole;
                        Logger.Info($"✅ Rol asignado localmente: {userRole}");
                        sim.SetUserRole(userRole);
                    }
                }
                return;
            }

            OnMessage.Invoke(message);
        }

        private static byte[] PreparePayload(string message)
        {
            var bytes = Encoding.UTF8.GetBytes(message);
            if (bytes.Length < 512)
            {
                return bytes;
            }

            try
            {
                using var output = new MemoryStream();
                using (var gzip = new GZipStream(output, CompressionLevel.Fastest, leaveOpen: true))
                {
                    gzip.Write(bytes, 0, bytes.Length);
                }

                var compressed = Convert.ToBase64String(output.ToArray());
                return Encoding.UTF8.GetBytes("gz:" + compressed);
            }
            catch
            {
                return bytes;
            }
        }

        private static string MaybeDecompress(string message)
        {
            if (!message.StartsWith("gz:", StringComparison.Ordinal))
            {
                return message;
            }

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
