using System;
using WebSocketSharp;

namespace SharedCockpitClient.Network
{
    public class WebSocketManager
    {
        private WebSocket? ws; // ✅ Nullable: se inicializa en Connect()
        private readonly string url;

        public event Action? OnOpen;
        public event Action<string>? OnMessage;
        public event Action<string>? OnError;
        public event Action? OnClose;

        public WebSocketManager(string serverUrl)
        {
            url = serverUrl ?? throw new ArgumentNullException(nameof(serverUrl));
        }

        public void Connect()
        {
            try
            {
                ws = new WebSocket(url);

                ws.OnOpen += (_, _) => OnOpen?.Invoke();
                ws.OnMessage += (_, e) => OnMessage?.Invoke(e.Data);
                ws.OnError += (_, e) => OnError?.Invoke(e.Message);
                ws.OnClose += (_, _) => OnClose?.Invoke();

                ws.Connect();
                SharedCockpitClient.Utils.Logger.Info($"🌐 Conectado al servidor WebSocket: {url}");
            }
            catch (Exception ex)
            {
                SharedCockpitClient.Utils.Logger.Error($"❌ Error al conectar WebSocket: {ex.Message}");
                OnError?.Invoke(ex.Message);
            }
        }

        public void Send(string message)
        {
            if (ws is { IsAlive: true })
            {
                ws.Send(message);
            }
            else
            {
                SharedCockpitClient.Utils.Logger.Warn("⚠️ No se puede enviar el mensaje: WebSocket no está conectado o es nulo.");
            }
        }

        public void Close()
        {
            if (ws == null) return;

            try
            {
                ws.Close();
                SharedCockpitClient.Utils.Logger.Info("🔌 Conexión WebSocket cerrada correctamente.");
            }
            catch (Exception ex)
            {
                SharedCockpitClient.Utils.Logger.Warn($"⚠️ Error al cerrar WebSocket: {ex.Message}");
            }
        }
    }
}
