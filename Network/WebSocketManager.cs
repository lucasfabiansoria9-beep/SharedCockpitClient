using System;
using SharedCockpitClient.Utils;
using WebSocketSharp;

namespace SharedCockpitClient.Network;

public class WebSocketManager
{
    private WebSocket? ws; // ✅ Nullable: se inicializa en Connect()
    private readonly string url;

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
        try
        {
            ws = new WebSocket(url);

            ws.OnOpen += (sender, e) => OnOpen.Invoke();
            ws.OnMessage += (sender, e) => OnMessage.Invoke(e.Data);
            ws.OnError += (sender, e) => OnError.Invoke(e.Message);
            ws.OnClose += (sender, e) => OnClose.Invoke();

            ws.Connect();
            Logger.Info($"Conectado al servidor WebSocket: {url}");
        }
        catch (Exception ex)
        {
            Logger.Error($"Error al conectar WebSocket: {ex.Message}");
            OnError.Invoke(ex.Message);
        }
    }

    public void Send(string message)
    {
        if (ws != null && ws.IsAlive)
        {
            ws.Send(message);
        }
        else
        {
            Logger.Warn("No se puede enviar el mensaje: WebSocket no está conectado o es nulo.");
        }
    }

    public void Close()
    {
        if (ws != null)
        {
            try
            {
                ws.Close();
                Logger.Info("Conexión WebSocket cerrada correctamente.");
            }
            catch (Exception ex)
            {
                Logger.Warn($"Error al cerrar WebSocket: {ex.Message}");
            }
        }
    }
}
