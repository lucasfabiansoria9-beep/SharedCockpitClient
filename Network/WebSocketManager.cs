using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SharedCockpitClient.Utils;

namespace SharedCockpitClient.Network;

public class WebSocketManager : IDisposable
{
    private readonly string url;
    private ClientWebSocket? client;
    private CancellationTokenSource? cts;
    private Task? workerTask;
    private bool disposed;

    public event Action OnOpen = delegate { };
    public event Action<string> OnMessage = delegate { };
    public event Action<string> OnError = delegate { };
    public event Action OnClose = delegate { };

    public WebSocketManager(string serverUrl)
    {
        url = serverUrl ?? throw new ArgumentNullException(nameof(serverUrl));
    }

    public void Connect()
    {
        if (disposed)
        {
            return;
        }

        if (workerTask != null && !workerTask.IsCompleted)
        {
            Logger.Warn("La conexión WebSocket ya está en curso.");
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
            Logger.Warn("No se puede enviar el mensaje: WebSocket no está conectado o es nulo.");
            return;
        }

        var payload = Encoding.UTF8.GetBytes(message);
        _ = SendInternalAsync(socket, payload);
    }

    public void Close()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;

        try
        {
            cts?.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // Ignorar.
        }

        if (client != null)
        {
            try
            {
                if (client.State is WebSocketState.Open or WebSocketState.CloseReceived)
                {
                    client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Cierre solicitado", CancellationToken.None)
                        .AsTask().Wait(TimeSpan.FromSeconds(1));
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"Error al cerrar WebSocket: {ex.Message}");
            }
            finally
            {
                client.Dispose();
            }
        }

        try
        {
            workerTask?.Wait(TimeSpan.FromSeconds(2));
        }
        catch (AggregateException ex)
        {
            Logger.Warn($"⚠️ Error al detener tarea WebSocket: {ex.Flatten().InnerException?.Message}");
        }

        cts?.Dispose();
        client = null;
        workerTask = null;
        OnClose.Invoke();
        Logger.Info("Conexión WebSocket cerrada correctamente.");
    }

    public void Dispose()
    {
        Close();
    }

    private async Task RunAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            using var ws = new ClientWebSocket();
            client = ws;

            try
            {
                await ws.ConnectAsync(new Uri(url), token).ConfigureAwait(false);
                Logger.Info($"Conectado al servidor WebSocket: {url}");
                OnOpen.Invoke();
                await ReceiveLoopAsync(ws, token).ConfigureAwait(false);

                if (ws.State == WebSocketState.CloseReceived)
                {
                    await ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Cierre reconocido", CancellationToken.None)
                        .ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error al conectar WebSocket: {ex.Message}");
                OnError.Invoke(ex.Message);
            }

            client = null;

            if (token.IsCancellationRequested)
            {
                break;
            }

            if (ws.State != WebSocketState.Open && ws.State != WebSocketState.Connecting)
            {
                OnClose.Invoke();
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(2), token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            Logger.Warn("Intentando reconectar al servidor WebSocket...");
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
                    {
                        return;
                    }

                    if (result.MessageType != WebSocketMessageType.Text)
                    {
                        continue;
                    }

                    builder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                }
                while (!result.EndOfMessage);

                if (builder.Length > 0)
                {
                    OnMessage.Invoke(builder.ToString());
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Cancelado.
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
            await socket.SendAsync(new ArraySegment<byte>(payload), WebSocketMessageType.Text, true, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.Warn($"Error enviando mensaje WebSocket: {ex.Message}");
            OnError.Invoke(ex.Message);
        }
    }
}
